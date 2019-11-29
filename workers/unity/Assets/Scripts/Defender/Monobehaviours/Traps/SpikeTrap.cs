using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Defender.Monobehaviours.Traps
{
    [RequireComponent(typeof(TrapBehaviour))]
    public class SpikeTrap : MonoBehaviour, ITrap
    {
        [Require] StructureSchema.TrapReader trapReader = null;

        public void ProcessTrapTriggered(List<EntityId> enemyIds)
        {
            StartCoroutine(PlayTrapAnimation());
            LinkedEntityComponent linkedEntityComponent = GetComponent<LinkedEntityComponent>();
            CommandSystem commandSystem = linkedEntityComponent.World.GetExistingSystem<CommandSystem>();
            foreach (EntityId enemyId in enemyIds)
            {
                commandSystem.SendCommand(new StatSchema.Stats.DamageEntity.Request
                {
                    TargetEntityId = enemyId,
                    Payload = new StatSchema.DamageRequest
                    {
                        Damage = trapReader.Data.Damage
                    }
                });
            }
        }

        public IEnumerator PlayTrapAnimation()
        {
            // Activate trap
            yield return null;
            // Activate cooldown.
        }
    }
}