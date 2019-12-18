using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.MonoBehaviours;
using MdgSchema.Common;
using MdgSchema.Common.Point;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StatSchema = MdgSchema.Common.Stats;
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