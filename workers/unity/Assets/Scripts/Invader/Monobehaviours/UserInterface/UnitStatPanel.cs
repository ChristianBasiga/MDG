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

public class UnitStatPanel : MonoBehaviour, IStatPanel
{
    ClientGameObjectCreator clientGameObjectCreator;
    LinkedEntityComponent currentlyLinkedEntity;
    ComponentUpdateSystem componentUpdateSystem;
    EntityManager entityManager;
    #region UI objects for this panel
    Image healthbar;

    #endregion


    
    // Start is called before the first frame update
    void Start()
    {
        clientGameObjectCreator = GameObject.Find("ClientWorker").GetComponent<UnityClientConnector>().clientGameObjectCreator;
    }

    public void SetEntityTracking(EntityId entityId)
    {
        this.gameObject.SetActive(true);
        GameObject linkedObject = clientGameObjectCreator.GetLinkedGameObjectById(entityId);
        currentlyLinkedEntity = linkedObject.GetComponent<LinkedEntityComponent>();

        componentUpdateSystem = currentlyLinkedEntity.World.GetExistingSystem<ComponentUpdateSystem>();

        // Attach to its synchronization component, or stat sync component. event to event.
        // so either that, all extend from synchronization, then attach to those events.
        // either that or every frame check for component updates
        currentlyLinkedEntity.Worker.TryGetEntity(entityId, out Entity entity);
        entityManager = currentlyLinkedEntity.World.EntityManager;

        // Then extract needed data to dislay
    }

    private void Update()
    {
        var statComponentUpdates = componentUpdateSystem.GetEntityComponentUpdatesReceived<StatSchema.Stats.Update>(currentlyLinkedEntity.EntityId);
        if (statComponentUpdates.Count > 0)
        {
            ref readonly var update = ref statComponentUpdates[0];

            UpdateHealthUI(update.Update.Health);
            // Other updates

        }
    }

    private void UpdateHealthUI(int newHealth)
    {
        currentlyLinkedEntity.Worker.TryGetEntity(currentlyLinkedEntity.EntityId, out Entity entity);

        StatSchema.StatsMetadata.Component statsMetadata = entityManager.GetComponentData<StatSchema.StatsMetadata.Component>(entity);

        float percentageHealth = newHealth / (float)statsMetadata.Health;
        StartCoroutine(HelperFunctions.UpdateHealthBar(healthbar, percentageHealth));
    }

}
