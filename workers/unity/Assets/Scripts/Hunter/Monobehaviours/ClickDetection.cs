using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Core;
using MDG.Common.Components;
namespace MDG.Hunter.Monobehaviours {
    /*
    public class ClickDetection : MonoBehaviour
    {
        public Entity hunter;

        void Update()
        {
            //I mean this could be moved to system now.
            bool clickedLeft = Input.GetMouseButtonDown(0);
            bool clickedRight = Input.GetMouseButtonDown(1);
            clickedRight = Input.GetKeyDown(KeyCode.R);
            Vector3 pos = Input.mousePosition;
            EntityManager entityManager = World.Active.EntityManager;

            if (clickedLeft || clickedRight)
            {
                Ray ray = Camera.main.ScreenPointToRay(pos);
                RaycastHit raycastHit;
                EntityId clickedEntityId = new EntityId(-1);
                if (Physics.Raycast(ray, out raycastHit))
                {
                    // Toggle the clicked Entity
                    ClickableMonobehaviour clickable = raycastHit.transform.GetComponent<ClickableMonobehaviour>();
                    if (clickable != null)
                    {
                        SpatialEntityId spatialEntityClickable = entityManager.GetComponentData<SpatialEntityId>(clickable.entity);

                        clickedEntityId = spatialEntityClickable.EntityId;

                        SpatialEntityId spatialEntityId = entityManager.GetComponentData<SpatialEntityId>(hunter);

                        clickable.Clicked(spatialEntityId.EntityId);
                    }
                }
                entityManager.SetComponentData(hunter, new MouseInputComponent
                {
                    DidClickThisFrame = true,
                    LeftClick = clickedLeft,
                    RightClick = clickedRight,
                    LastClickedPos = pos,
                    SelectedEntityId = clickedEntityId
                });

                ClickableMonobehaviour[] clickables = FindObjectsOfType<ClickableMonobehaviour>();
                foreach (ClickableMonobehaviour noLongerClicked in clickables)
                {
                    SpatialEntityId clickableEntityId = entityManager.GetComponentData<SpatialEntityId>(noLongerClicked.entity);
                    if (!clickableEntityId.EntityId.Equals(clickedEntityId))
                    {
                        noLongerClicked.NoLongerClicked();
                    }
                }
            }
            else
            {
                
                MouseInputComponent previousState = entityManager.GetComponentData<MouseInputComponent>(hunter);
                entityManager.SetComponentData(hunter, new MouseInputComponent
                {
                    LastClickedPos = previousState.LastClickedPos,
                    SelectedEntityId = previousState.SelectedEntityId,
                    RightClick = previousState.RightClick,
                    LeftClick = previousState.LeftClick,
                    DidClickThisFrame = false,
                });
            }

           
        }

        
    }*/
}