using Improbable.Gdk.Subscriptions;
using UnityEngine;
namespace MDG.Defender.Monobehaviours
{
    public class TeamStatusUpdater : MonoBehaviour
    {
        TeammatePanel[] teammatePanels;
        int teammatesLoaded = 0;
        ClientGameObjectCreator clientGameObjectCreator;

        // Start is called before the first frame update
        void Start()
        {
            teammatePanels = GetComponentsInChildren<TeammatePanel>();
        }

        public void AddTeammate(LinkedEntityComponent linkedDefender)
        {
            teammatePanels[teammatesLoaded++].SetPlayer(linkedDefender);
        }
    }
}