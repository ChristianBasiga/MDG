using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MDG.Common.MonoBehaviours;
using Improbable.Gdk.Core;
using MDG;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using Unity.Entities;
using StatSchema = MdgSchema.Common.Stats;
using MdgSchema.Common;

namespace MDG.Invader.Monobehaviours
{
    public class UnitStatPanel : MonoBehaviour, IStatPanel
    {
        ClientGameObjectCreator clientGameObjectCreator;
        LinkedEntityComponent currentlyLinkedEntity;
        ComponentUpdateSystem componentUpdateSystem;
        #region UI objects for this panel
        Image healthbar;

        public GameEntityTypes GetGameEntityType() {
            return GameEntityTypes.Unit;
        }
        #endregion



        // Start is called before the first frame update
        void Start()
        {
            clientGameObjectCreator = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().clientGameObjectCreator;
            healthbar = GameObject.Find("StatPanelHealthBar").GetComponent<Image>();
        }

        public void SetEntityTracking(EntityId entityId)
        {
            this.gameObject.SetActive(true);
            GameObject linkedObject = clientGameObjectCreator.GetLinkedGameObjectById(entityId);
            currentlyLinkedEntity = linkedObject.GetComponent<LinkedEntityComponent>();
            componentUpdateSystem = currentlyLinkedEntity.World.GetExistingSystem<ComponentUpdateSystem>();
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                var statComponentUpdates = componentUpdateSystem.GetEntityComponentUpdatesReceived<StatSchema.Stats.Update>(currentlyLinkedEntity.EntityId);
                if (statComponentUpdates.Count > 0)
                {
                    ref readonly var update = ref statComponentUpdates[0];

                    UpdateHealthUI(update.Update.Health);
                    // Other updates

                }
            }
        }

        private void UpdateHealthUI(int newHealth)
        {
            currentlyLinkedEntity.Worker.TryGetEntity(currentlyLinkedEntity.EntityId, out Entity entity);

            StatSchema.StatsMetadata.Component statsMetadata = componentUpdateSystem.EntityManager.GetComponentData<StatSchema.StatsMetadata.Component>(entity);

            float percentageHealth = newHealth / (float)statsMetadata.Health;
            StartCoroutine(HelperFunctions.UpdateFill(healthbar, percentageHealth));
        }

        public void Disable()
        {
            this.gameObject.SetActive(false);
        }
    }
}