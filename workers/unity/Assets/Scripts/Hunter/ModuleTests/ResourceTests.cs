using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hunter = MDG.Hunter;
using ResourceSchema = MdgSchema.Game.Resource;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core.Commands;
using MDG.Game;

public class ResourceTests : MonoBehaviour
{
    CommandSystem commandSystem;
    EntityId resourceManagerEntityId = new EntityId(4);
    EntityId unitId = new EntityId(200);
    EntityId resourceEntityId = new EntityId(100);
    string workerType;
    // Simply receive input to send requests
    // Start is called before the first frame update
    void Start()
    {
     //   resource = GameObject.FindGameObjectWithTag("Resource").GetComponent<LinkedEntityComponent>().EntityId;
    }

    // Update is called once per frame
    void Update()
    {
        // Should work, but replace direct creation 
        if (commandSystem == null && GetComponent<MDG.UnityClientConnector>().Worker != null )
        {
            commandSystem = GetComponent<MDG.UnityClientConnector>().Worker.World.GetExistingSystem<CommandSystem>();
        }
        // Spawn Unit.
        if (Input.GetKeyDown(KeyCode.W))
        {
            string workerType = GetComponent<MDG.UnityClientConnector>().Worker.WorkerType;
            commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(MDG.Hunter.Unit.Templates.GetCollectorUnitEntityTemplate(workerType)));
        }
        // Spawn resource
        if (Input.GetKeyDown(KeyCode.Q))
        {
            commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(Templates.CreateResourceEntityTemplate()));
        }

       
        // Occupy test.
        if (Input.GetKeyDown(KeyCode.A))
        {

            resourceEntityId = GameObject.FindGameObjectWithTag("Resource").GetComponent<LinkedEntityComponent>().EntityId;
            unitId = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>().EntityId;
            commandSystem.SendCommand(new ResourceSchema.ResourceManager.Occupy.Request
            {
                TargetEntityId = resourceManagerEntityId,
                Payload = new ResourceSchema.OccupyRequest
                {
                    Occupying = unitId,
                    ToOccupy = resourceEntityId
                }
            });
        }
        // Collect test.
        else if (Input.GetKeyDown(KeyCode.S))
        {
            resourceEntityId = GameObject.FindGameObjectWithTag("Resource").GetComponent<LinkedEntityComponent>().EntityId;
            unitId = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>().EntityId;
            commandSystem.SendCommand(new ResourceSchema.ResourceManager.Collect.Request
            {
                TargetEntityId = resourceManagerEntityId,
                Payload = new ResourceSchema.CollectRequest
                {
                    CollectorId = unitId,
                    ResourceId = resourceEntityId
                }
            });
        }
        // Release test.
        else if (Input.GetKeyDown(KeyCode.D))
        {
            resourceEntityId = GameObject.FindGameObjectWithTag("Resource").GetComponent<LinkedEntityComponent>().EntityId;
            unitId = GameObject.FindGameObjectWithTag("Unit").GetComponent<LinkedEntityComponent>().EntityId;
            commandSystem.SendCommand(new ResourceSchema.ResourceManager.Release.Request
            {
                TargetEntityId = resourceManagerEntityId,
                Payload = new ResourceSchema.ReleaseRequest
                {
                    Occupant = unitId,
                    ResourceId = resourceEntityId
                }
            });
        }
    }
}
