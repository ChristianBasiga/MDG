using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using MDG.Common.Components;
using MDG.Hunter.Monobehaviours;
using Improbable.Gdk.Core;
using Unity.Collections;
using UnityEngine.Jobs;
using MDG.Hunter.Components;
using MDG.Hunter.Systems;
using Improbable.Gdk.Subscriptions;

namespace MDG.Common.Systems {

    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class MouseInputSystem : JobComponentSystem
    {
        private JobHandle clickedToggleJobHandle;
        bool scheduledToggleHandle;
        public struct ClickDetectionJob : IJobForEach<MouseInputComponent, SpatialEntityId, CommandGiver>
        {
            [WriteOnly]
            public NativeArray<EntityId> entitiesSelected;
            public int selectedIndex;
            public bool clickedLeft;
            public bool clickedRight;
            public Vector3 mousePosition;
            [WriteOnly]
            public NativeArray<EntityId> selector;
            [WriteOnly]
            public NativeArray<EntityId> rightClicked;
            public EntityId clickableEntityHit;
            public void Execute(ref MouseInputComponent c0, [ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref CommandGiver commandGiver)
            {
                EntityManager entityManager = World.Active.EntityManager;
                if (clickedLeft || clickedRight)
                {
                    if (clickableEntityHit.IsValid())
                    {
                        c0.SelectedEntityId = clickableEntityHit;
                    }
                    else if (!clickedRight) //Click right would mean moving, so if clicked left and clicked nothing, deselect.
                    {
                        c0.SelectedEntityId = new EntityId(-1);
                    }
                    else //Otherwise if did click right, then mark right selection.
                    {
                        rightClicked[0] = spatialEntityId.EntityId;
                    }
                    c0.DidClickThisFrame = true;
                    c0.LeftClick = clickedLeft;
                    c0.RightClick = clickedRight;
                    c0.LastClickedPos = mousePosition;
                }
                //If didn't click this frame, then may still be selected.
                else if (commandGiver.SelectedListener.IsValid())
                {
                    entitiesSelected[selectedIndex++] = commandGiver.SelectedListener;
                }
                // Down the line prob not though since only one clicker and this is overkill.
                // If go with multiple command givers, this will be indexed instead.
                selector[0] = spatialEntityId.EntityId;
            }
        }

        //Also don't toggle off if selected by commandGiver.
        public struct ClickToggleJob : IJobForEach<Clickable, SpatialEntityId>
        {
            public EntityId selector;
            public EntityId selected;
            public EntityId rightClicked;
            public EntityId selectedByCommandGiver;
            // Later on there may be multiple and update accordingly, but for now
            // for testing purposes there is only one commandGiver. Same with selector.
            public void Execute(ref Clickable c0, [ReadOnly] ref SpatialEntityId c1)
            {
                // Need to add check to make sure not equal to selec
                if (selected.IsValid() && c1.EntityId.Equals(selected)){
                    c0.Clicked = true;
                    c0.ClickedEntityId = selector;
                }
                else if (c1.EntityId.Equals(rightClicked)){
                    c0.Clicked = true;
                }
                else if (!selectedByCommandGiver.Equals(c1.EntityId))
                {
                    c0.Clicked = false;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            bool clickedLeft = Input.GetMouseButtonDown(0);
            bool clickedRight = Input.GetMouseButtonDown(1);
            if (!clickedLeft && !clickedRight) return inputDeps;

            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            //Assume nothing clicked
            EntityId clickableEntityHit = new EntityId(-1);
            EntityManager entityManager = World.Active.EntityManager;

            //Check if clicked anything and get clicked entity id.
            if (Physics.Raycast(mouseRay, out RaycastHit raycastHit))
            {
                // Gotta add extra check for validity later.
                LinkedEntityComponent clickable = raycastHit.transform.GetComponent<LinkedEntityComponent>();
                if (clickable != null)
                {
                    clickableEntityHit = clickable.EntityId;
                }
            }
            // Selected by giver and click detection can run in parallel both just have to finish before 
            // Click Toggle Job.
            NativeArray<EntityId> selectedByGiver = new NativeArray<EntityId>(1, Allocator.TempJob);
            // Detect left clicked and right clicks.
            NativeArray<EntityId> selector = new NativeArray<EntityId>(1, Allocator.TempJob);
            NativeArray<EntityId> rightClicked = new NativeArray<EntityId>(1, Allocator.TempJob);
            ClickDetectionJob clickDetectionJob = new ClickDetectionJob
            {
                clickedLeft = clickedLeft,
                clickedRight = clickedRight,
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition),
                clickableEntityHit = clickableEntityHit,
                selector = selector,
                rightClicked = rightClicked,
                entitiesSelected = selectedByGiver,
                selectedIndex = 0
            };
            JobHandle detectionHandle = clickDetectionJob.Schedule(this,inputDeps);
            detectionHandle.Complete();
            // If not completed, force it to complete so can run again
            // this frame with updated toggles.
            if (scheduledToggleHandle) {
                clickedToggleJobHandle.Complete();
                scheduledToggleHandle = true;
            }
            ClickToggleJob clickToggleJob = new ClickToggleJob
            {
                selector = selector[0],
                selected = clickableEntityHit,
                rightClicked = rightClicked[0],
                selectedByCommandGiver = selectedByGiver[0]
            };
            clickedToggleJobHandle = clickToggleJob.Schedule(this, detectionHandle);
            scheduledToggleHandle = true;
            selector.Dispose();
            selectedByGiver.Dispose();
            rightClicked.Dispose();
            return clickedToggleJobHandle;
        }
    }
}