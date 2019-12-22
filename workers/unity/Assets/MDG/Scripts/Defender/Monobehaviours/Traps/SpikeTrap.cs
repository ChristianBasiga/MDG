using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using Unity.Entities;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using StructureSchema = MdgSchema.Common.Structure;

namespace MDG.Defender.Monobehaviours.Traps
{
    [RequireComponent(typeof(TrapBehaviour))]
    // Do need auth and non auth.
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
                Debug.Log($"Sending damage of {trapReader.Data.Damage} from Spike trap");
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

        // Animations will be on monobehaviour on both authortiative and non authoritative cients.,
        public IEnumerator PlayTrapAnimation()
        {
            // Activate trap
            yield return null;
            // Activate cooldown.
        }
    }
}