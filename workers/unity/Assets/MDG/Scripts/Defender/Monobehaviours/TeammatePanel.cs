using Improbable.Gdk.Subscriptions;
using MDG.Common.MonoBehaviours;
using MdgSchema.Player;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace MDG.Defender.Monobehaviours
{
    public class TeammatePanel : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Image healthbar;
        [SerializeField]
        Text playerName;
        [SerializeField]
        Image playerIcon;
#pragma warning restore 649

        EntityManager entityManager;
        // Maybe like playerconfig.
        public void SetPlayer(LinkedEntityComponent linkedEntityComponent)
        {
            HealthSynchronizer healthSynchronizer = linkedEntityComponent.GetComponent<HealthSynchronizer>();
            linkedEntityComponent.Worker.TryGetEntity(linkedEntityComponent.EntityId, out Entity entity);
            PlayerMetaData.Component playerMetaData = linkedEntityComponent.World.EntityManager.GetComponentData<PlayerMetaData.Component>(entity);
            playerName.text = playerMetaData.UserName;
            healthSynchronizer.OnHealthBarUpdated += UpdateHealthBar;
        }

        private void UpdateHealthBar(int fill)
        {
            healthbar.fillAmount = fill;
        }

    }
}