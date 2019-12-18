using Improbable.Gdk.Subscriptions;
using UnityEngine;
using UnityEngine.UI;
using TerritorySchema = MdgSchema.Game.Territory;
namespace MDG.Common.MonoBehaviours.Synchronizers
{
    public class TerritorySynchronizer : MonoBehaviour
    {

        [SerializeField]
        Image claimedImage;
        [SerializeField]
        Image claimingImage;
        [SerializeField]
        Image freeImage;

        // Add warning drowner for this later.
        [Require] TerritorySchema.TerritoryStatusReader territoryStatusReader;
        // Start is called before the first frame update
        void Start()
        {
            claimedImage.gameObject.SetActive(false);
            claimingImage.gameObject.SetActive(false);
            freeImage.gameObject.SetActive(false);
            territoryStatusReader.OnStatusUpdate += OnTerritoryStatusUpdate;
        }

        private void OnTerritoryStatusUpdate(TerritorySchema.TerritoryStatusTypes status)
        {
            claimedImage.gameObject.SetActive(false);
            claimingImage.gameObject.SetActive(false);
            freeImage.gameObject.SetActive(false);

            switch (status)
            {
                case TerritorySchema.TerritoryStatusTypes.Claimed:
                    claimedImage.gameObject.SetActive(true);
                    break;
                case TerritorySchema.TerritoryStatusTypes.Claiming:
                    claimingImage.gameObject.SetActive(true);
                    break;
                case TerritorySchema.TerritoryStatusTypes.Released:
                    freeImage.gameObject.SetActive(true);
                    break;
            }
        }
    }
}