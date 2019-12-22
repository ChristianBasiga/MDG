using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common.Components;
using MDG.Common.MonoBehaviours;
using MDG.Invader.Components;
using MdgSchema.Common;
using MdgSchema.Common.Util;
using MdgSchema.Units;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MDG.Invader.Systems {

    [DisableAutoCreation]
    [UpdateInGroup(typeof(EntitySelectionGroup))]
    [UpdateBefore(typeof(CommandGiveSystem))]
    [AlwaysUpdateSystem]
    public class SelectionSystem : ComponentSystem
    {
        public event Action<bool> OnUnitSelectionUpdated;
        JobHandle selectedJobHandle;
        EntityQuery selectorGroup;
        ClientGameObjectCreator clientGameObjectCreator;

        public struct SelectionBounds
        {
            public float3 botLeft;
            public float3 topRight;
        }


        SelectionBounds? selectionBounds;
        public bool SelectedThisFrame { get { return selectionBounds.HasValue; } }
        EntityQuery clickableUnitQuery;

        // Prob just use init instead of onstart running
        public void Init(ClientGameObjectCreator clientGameObjectCreator)
        {
            this.clientGameObjectCreator = clientGameObjectCreator;

        }

        public void SetSelectionBounds(SelectionBounds selectionBounds)
        {
            this.selectionBounds = selectionBounds;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            selectorGroup = GetEntityQuery(typeof(Selection));
            clickableUnitQuery = GetEntityQuery(
                ComponentType.ReadWrite<Clickable>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<EntityPosition.Component>(),
                ComponentType.ReadOnly<Unit.Component>());
        }



        /* Unless I have multiple invaders all in same client, need not be a job.
        public struct GetSelectedBounds : IJobForEach<SpatialEntityId, Selection>
        {
            [WriteOnly]
            public NativeHashMap<EntityId, SelectionBounds>.ParallelWriter idToSelectionBounds;
            public void Execute([ReadOnly] ref SpatialEntityId spatialEntityId, [ReadOnly] ref Selection selection)
            {
                // Gotta apply rotation here.
                float3 botLeft = new float3(math.min(selection.StartPosition.x, selection.EndPosition.x), math.min(selection.StartPosition.z, selection.EndPosition.z), 0);
                float3 topRight = new float3(math.max(selection.StartPosition.x, selection.EndPosition.x), math.max(selection.StartPosition.z, selection.EndPosition.z), 0);
                
                float selectionArea = math.distance(botLeft, topRight);
                idToSelectionBounds.TryAdd(spatialEntityId.EntityId, new SelectionBounds
                {
                    botLeft = botLeft,
                    topRight = topRight,
                });
            }
        }*/

        public struct RemoveSelections : IJobForEachWithEntity<Selection>
        {
            public EntityCommandBuffer.Concurrent commandBuffer;
            public void Execute(Entity entity, int index, ref Selection c0)
            {
                commandBuffer.RemoveComponent(index, entity, typeof(Selection));
            }
        }

        public struct ResetSelectedEntities : IJobForEach<Clickable>
        {
            public void Execute(ref Clickable c0)
            {
                c0.Clicked = false;
                c0.ClickedEntityId = new EntityId(-1);
            }
        }

        public struct SetSelectedEntities : IJobForEach<SpatialEntityId, EntityPosition.Component, Clickable, Unit.Component>
        {
            public EntityId invaderId;
            public SelectionBounds selectionBounds;

            public NativeQueue<bool>.ParallelWriter selectedQueue;
            // Instead of single, do double buffering, have a selected to read from, and one to writ eto?
            public void Execute([ReadOnly] ref SpatialEntityId id, [ReadOnly] ref EntityPosition.Component EntityPosition, ref Clickable clickable,
                [ReadOnly] ref Unit.Component unitComponent)
            {
                Vector3f position = EntityPosition.Position;
                // Check if position of clickable entity is within selection bounds.
                if (position.X > selectionBounds.botLeft.x && position.Z > selectionBounds.botLeft.y
                    && position.X < selectionBounds.topRight.x && position.Z < selectionBounds.topRight.y)
                {
                    clickable.Clicked = true;
                    clickable.ClickedEntityId = invaderId;
                    UnityEngine.Debug.Log("adding to selected queue");
                    selectedQueue.Enqueue(true);
                }
            }
        }
        protected override void OnUpdate()
        {
            if (!selectionBounds.HasValue)
            {
                return;
            }
            ResetSelectedEntities resetSelectedEntitiesJob = new ResetSelectedEntities();
            resetSelectedEntitiesJob.Schedule(this).Complete();

            NativeQueue<bool> selected = new NativeQueue<bool>(Allocator.TempJob);
            bool selectedThroughMono = false;
            Entities.ForEach((ref SpatialEntityId spatialEntityId, ref Clickable clickable) =>
            {
                ClickableMonobehaviour clickableMonobehaviour = clientGameObjectCreator.GetLinkedGameObjectById(spatialEntityId.EntityId).GetComponent<ClickableMonobehaviour>();
                if (clickableMonobehaviour != null && clickableMonobehaviour.SelectedThisFrame)
                {
                    selectedThroughMono = true;
                    clickable.Clicked = true;
                    clickable.ClickedEntityId = clientGameObjectCreator.PlayerLink.EntityId;
                }
            });

            SetSelectedEntities setSelectedEntities = new SetSelectedEntities
            {
                selectionBounds = selectionBounds.Value,
                selectedQueue = selected.AsParallelWriter(),
                invaderId = clientGameObjectCreator.PlayerLink.EntityId
            };
            selectedJobHandle = setSelectedEntities.Schedule(clickableUnitQuery);
            selectedJobHandle.Complete();
            OnUnitSelectionUpdated?.Invoke(selected.Count > 0 || selectedThroughMono);
            selected.Dispose();
            selectionBounds = null;

            RemoveSelections removeSelections = new RemoveSelections
            {
                commandBuffer = PostUpdateCommands.ToConcurrent()
            };
            removeSelections.Schedule(this).Complete();
        }
    }
}