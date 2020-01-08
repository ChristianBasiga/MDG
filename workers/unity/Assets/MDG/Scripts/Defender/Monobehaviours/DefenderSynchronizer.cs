using Improbable.Gdk.Subscriptions;
using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using MDG.Common.MonoBehaviours.Synchronizers;
using MdgSchema.Common;
using System.Collections;
using UnityEngine;
using SpawnSchema = MdgSchema.Common.Spawn;
using StatSchema = MdgSchema.Common.Stats;

namespace MDG.Defender.Monobehaviours
{
    // Finish synchronization of defender.
    // Change this to PlayerSynchronizer as common end game behaviour
    public class DefenderSynchronizer : MonoBehaviour, IPlayerSynchronizer
    {
#pragma warning disable 649
        [Require] SpawnSchema.PendingRespawnReader pendingRespawnReader;
        [Require] StatSchema.StatsReader statsReader;
        [Require] StatSchema.StatsMetadataReader statsMetadataReader;
#pragma warning restore 649

        public DefenderHUD DefenderHUD { private set; get; }
        TeamStatusUpdater teamStatusUpdater;
        public UnityClientConnector ClientWorker { private set; get; }

        private void Start()
        {
            pendingRespawnReader.OnRespawnActiveUpdate += OnRespawnActiveChange;
            pendingRespawnReader.OnTimeTillRespawnUpdate += OnPendingRespawnTimerUpdate;
            StartCoroutine(InitUIRefs());
            statsReader.OnHealthUpdate += OnHealthUpdate;
        }

        private void OnPendingRespawnTimerUpdate(float time)
        {
            DefenderHUD.OnUpdateRespawn(time);
        }

        private IEnumerator InitUIRefs()
        {
            yield return new WaitUntil(() => ClientWorker != null && ClientWorker.LoadedUI != null);
            // This method won't scale well.
            ClientWorker.LoadedUI.TryGetValue("DefenderHud", out GameObject hudObject);
            DefenderHUD = hudObject.GetComponent<DefenderHUD>();

            ClientWorker.LoadedUI.TryGetValue("TeammateCanvas", out GameObject statusUpdater);
            teamStatusUpdater = statusUpdater.transform.GetChild(0).GetComponent<TeamStatusUpdater>();
            var defenderLinks = ClientWorker.ClientGameObjectCreator.OtherPlayerLinks.FindAll((link) => link.CompareTag(GameEntityTypes.Defender.ToString()));
            for (int i = 0; i < defenderLinks.Count; ++i)
            {
                teamStatusUpdater.AddTeammate(defenderLinks[i]);
            }
        }



        private void OnEntityAdded(Improbable.Gdk.GameObjectCreation.SpatialOSEntity obj)
        {
            if (obj.TryGetComponent(out GameMetadata.Component gameMetadata) && gameMetadata.Type == GameEntityTypes.Defender)
            {
                GameObject linkedDefender = ClientWorker.ClientGameObjectCreator.GetLinkedGameObjectById(obj.SpatialOSEntityId);
                teamStatusUpdater.AddTeammate(linkedDefender.GetComponent<LinkedEntityComponent>());
            }
        }
        private void OnHealthUpdate(int currentHealth)
        {
            float pct = currentHealth / (float)statsMetadataReader.Data.Health;
            DefenderHUD.OnUpdateHealth(pct);
        }
        private void OnRespawnActiveChange(bool respawning)
        {
            gameObject.SetActive(!respawning);
            if (respawning)
            {
                DefenderHUD.OnRespawning();
            }
            else
            {
                DefenderHUD.OnDoneRespawning();
            }
        }

        public void LinkClientWorker(UnityClientConnector unityClientConnector)
        {
            ClientWorker = unityClientConnector;
            unityClientConnector.ClientGameObjectCreator.OnEntityAdded += OnEntityAdded;
            if (unityClientConnector.TryGetComponent(out GameStatusSynchronizer gameStatusSynchronizer))
            {
                if (TryGetComponent(out InputProcessorManager inputProcessorManager))
                {
                    inputProcessorManager.SetSynchronizer(gameStatusSynchronizer);
                }
                else
                {
                    Debug.LogError("Player synchronizer is missing input processor manager");
                }
            }
            else
            {
                Debug.LogError("Client worker is missing game status synchronizer.");
            }
        }
    }
}