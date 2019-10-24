using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StatSchema = MdgSchema.Common.Stats;
using CommonSchema = MdgSchema.Common;
using UnityEngine.UI;
using MDG.Common;
using Improbable.Gdk.Core;

namespace MDG.Common.MonoBehaviours
{
   
    public interface IStatPanel
    {
        void SetEntityTracking(EntityId entityId);
    }

    /// <summary>
    /// Panel that shows up upon clicking on a game entity with stats.
    /// </summary>
    public class StatPanel : MonoBehaviour, IStatPanel
    {
        Image healthbar;

        Dictionary<CommonSchema.GameEntityTypes, IStatPanel> typeToPanel;

        //Other UI objects for stats, whatever.

        // Start is called before the first frame update
        void Start()
        {
            typeToPanel = new Dictionary<CommonSchema.GameEntityTypes, IStatPanel>();
        }


        public void UpdatePanel(CommonSchema.GameEntityTypes gameEntityType, IStatPanel panel)
        {
            // Do this tomorrow.
        }

        // So they click on entity, or whatever to see stats. All we do is pass in entityId of component
        // from that we get the metadata, then from that we load in the specific stat panel.
        // Then that specific stat panel queries all neccessarry data needed to fill out panel.
        public void SetEntityTracking(EntityId entityId)
        {
            this.gameObject.SetActive(true);
        }

        public void SetPayload(StatSchema.Stats.Component currentStats, StatSchema.StatsMetadata.Component statMetaData)
        {

        }
    }
}