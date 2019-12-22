using Improbable.Gdk.Core;
using MdgSchema.Common;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using CommonSchema = MdgSchema.Common;

namespace MDG.Common.MonoBehaviours
{

    public interface IStatPanel
    {
        void SetEntityTracking(EntityId entityId);
        CommonSchema.GameEntityTypes GetGameEntityType();
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
            for ( int i = 0; i < transform.childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                IStatPanel statPanel = child.GetComponent<IStatPanel>();
                typeToPanel.Add(statPanel.GetGameEntityType(), statPanel);
            }
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