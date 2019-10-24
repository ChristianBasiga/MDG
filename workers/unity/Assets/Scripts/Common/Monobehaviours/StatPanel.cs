using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using CommonSchema = MdgSchema.Common;
using UnityEngine.UI;
using MDG.Common;
using Improbable.Gdk.Core;
using MdgSchema.Common;
using Unity.Entities;

namespace MDG.Common.MonoBehaviours
{
   
    public interface IStatPanel
    {
        void SetEntityTracking(EntityId entityId);
        void Disable();
    }

    /// <summary>
    /// Panel that shows up upon clicking on a game entity with stats.
    /// </summary>
    public class StatPanel : MonoBehaviour
    {
        Dictionary<CommonSchema.GameEntityTypes, IStatPanel> typeToPanel;
        UnityClientConnector unityClientConnector;

        IStatPanel active;

        void Start()
        {
            // Getting to point injection is needed.
            unityClientConnector = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>();
            typeToPanel = new Dictionary<CommonSchema.GameEntityTypes, IStatPanel>();
        }

        public void UpdatePanel(CommonSchema.GameEntityTypes gameEntityType, IStatPanel panel)
        {
            // Do this tomorrow
            typeToPanel[gameEntityType] = panel;
        }

        // Recursively sets entity tracking to panel matching metadata.
        public void SetEntityTracking(EntityId entityId)
        {
            if (active != null)
            {
                active.Disable();
            }
            WorkerSystem workerSystem = unityClientConnector.Worker.World.GetExistingSystem<WorkerSystem>();
            workerSystem.TryGetEntity(entityId, out Entity entity);
            GameMetadata.Component gameMetadataComponent = workerSystem.EntityManager.GetComponentData<GameMetadata.Component>(entity);
            IStatPanel statPanel = typeToPanel[gameMetadataComponent.Type];
            active = statPanel;
            statPanel.SetEntityTracking(entityId);
        }
    }
}