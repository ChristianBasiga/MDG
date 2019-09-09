using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Entities;
using Improbable.Gdk.Core;
using MdgSchema.Common;
namespace MDG.Common.Systems
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class StatUpdateSystem : ComponentSystem
    {
        private CommandSystem commandSystem;
        private WorkerSystem workerSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override void OnUpdate()
        {
            #region Process Damage requests
            var damageRequests = commandSystem.GetRequests<Stats.DamageEntity.ReceivedRequest>();
            Dictionary<EntityId, List<long>> entityIdToDamageRequests = new Dictionary<EntityId, List<long>>();
            
            for (int i = 0; i < damageRequests.Count; ++i)
            {
                ref readonly var request = ref damageRequests[i];
                //Instead of like this could make it a job later.
                Entity toUpdate;
                Debug.LogError($"To damage Id {request.Payload.ToDamage} with ${request.Payload.Damage}");
                if (workerSystem.TryGetEntity(request.Payload.ToDamage, out toUpdate))
                {
                    
                    Stats.Component stats = EntityManager.GetComponentData<Stats.Component>(toUpdate);
                    // Need to query on authoritative stat changes
                    EntityManager.SetComponentData(toUpdate, new Stats.Component { Health = stats.Health - request.Payload.Damage });
                    //Respective clients will handle death, this simply updates stats.
                    bool isDead = (stats.Health - request.Payload.Damage) <= 0;
                    Debug.LogError($"I happen and died {isDead}");
                    Debug.LogError($"Resulting health {stats.Health}");
                    commandSystem.SendResponse(new Stats.DamageEntity.Response { RequestId = request.RequestId, Payload = new DamageResponse { DidDie = isDead } });
                }
                else
                {
                    Debug.LogError("Couldn't get entity");
                }
                // For now be here to delete;
            }
            #endregion


        }

    }
}