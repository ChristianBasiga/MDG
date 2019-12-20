using Improbable.Gdk.Subscriptions;
using UnityEngine;
using UnityEngine.UI;
using TerritorySchema = MdgSchema.Game.Territory;
namespace MDG.Common.MonoBehaviours.Synchronizers
{
    public class TerritorySynchronizer : MonoBehaviour
    {


        Material baseMaterial;
#pragma warning disable 649
        [SerializeField]
        Material claimedImage;
        [SerializeField]
        Material claimingImage;
        [SerializeField]
        Material freeImage;

        [SerializeField]
        //Image claimProgress;
        [Require] TerritorySchema.TerritoryStatusReader territoryStatusReader;
#pragma warning restore 649

        MeshRenderer meshRenderer;

        // Start is called before the first frame update
        void Start()
        {
           // claimProgress.gameObject.SetActive(false);

            meshRenderer = GetComponent<MeshRenderer>();
            baseMaterial = meshRenderer.material;
            territoryStatusReader.OnStatusUpdate += OnTerritoryStatusUpdate;
            territoryStatusReader.OnClaimProgressUpdate += UpdateClaimProgressbar;
            // Add caim progress bar here too, might as well, this is already ui.
        }

        private void UpdateClaimProgressbar(float obj)
        {
            // suppose would be this.
            Debug.Log("Claim progress " + obj);
        }

        private void OnTerritoryStatusUpdate(TerritorySchema.TerritoryStatusTypes status)
        {
            switch (status)
            {
                case TerritorySchema.TerritoryStatusTypes.Claimed:
                    meshRenderer.material = claimedImage;
               //     claimProgress.gameObject.SetActive(false);
                    break;
                // Should be a bar.
                case TerritorySchema.TerritoryStatusTypes.Claiming:
                    meshRenderer.material = claimingImage;
                 //   claimProgress.gameObject.SetActive(true);
                    break;
                case TerritorySchema.TerritoryStatusTypes.Released:
                    meshRenderer.material = baseMaterial;
                   // claimProgress.gameObject.SetActive(false);
                    break;
            }
        }
    }
}