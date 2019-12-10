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
    public class DefenderHUD : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Text errorText;
        [Require] PointReader pointReader;
        // These shouldn't be as a child of player. Later on have ui oader like I dow ith structure behaviour
        // to load those in.
        [SerializeField]
        TeammatePanel[] teammatePanels;
#pragma warning restore 649

        MainOverlayHUD mainOverlayHUD;



      
        int teammatesLoaded = 0;

      

        bool newErrorPassed = false;
        string text;
        IEnumerator errorClearRoutine;

        ClientGameObjectCreator clientGameObjectCreator;

        // Start is called before the first frame update
        void Start()
        {
            GameObject clientWorker = GameObject.Find("ClientWorker");
            mainOverlayHUD = clientWorker.GetComponent<MainOverlayHUD>();

            UnityClientConnector unityClientConnector = clientWorker.GetComponent<UnityClientConnector>();
            clientGameObjectCreator = unityClientConnector.ClientGameObjectCreator;

            var defenderLinks = clientGameObjectCreator.otherPlayerLinks.FindAll((link) =>link.TryGetComponent(typeof(DefenderSynchronizer), out _));
            for (int i = 0; i < defenderLinks.Count; ++i)
            {
                teammatePanels[teammatesLoaded++].SetPlayer(defenderLinks[i]);
                
            }
            if (teammatesLoaded != teammatePanels.Length)
            {
                clientGameObjectCreator.OnEntityAdded += OnEntityAdded;
            }
            // Subsribe to main overlay hud.
            DefenderSynchronizer defenderSynchronizer = GetComponent<DefenderSynchronizer>();
            defenderSynchronizer.OnLoseGame += DisplayLoseGameUI;
            defenderSynchronizer.OnWinGame += DisplayWinGameUI;
            pointReader.OnValueUpdate += mainOverlayHUD.UpdatePoints;
            errorText.gameObject.SetActive(false);




        }

        private void OnEntityAdded(Improbable.Gdk.GameObjectCreation.SpatialOSEntity obj)
        {
            if (teammatesLoaded < teammatePanels.Length && obj.TryGetComponent(out GameMetadata.Component gameMetadata) && gameMetadata.Type == GameEntityTypes.Hunted)
            {
                GameObject linkedDefender = clientGameObjectCreator.GetLinkedGameObjectById(obj.SpatialOSEntityId);
                if (linkedDefender.CompareTag("Player"))
                {
                    return;
                }
                teammatePanels[teammatesLoaded++].SetPlayer(linkedDefender.GetComponent<LinkedEntityComponent>());
            }
        }

        // Need more granular way to do this.
        public void SetErrorText(string errorMsg)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = errorMsg;
            if (errorClearRoutine == null)
            {
                errorClearRoutine = ClearError();
                StartCoroutine(errorClearRoutine);
            }
        }

        IEnumerator ClearError()
        {
            float timePassed = 0;
            while (timePassed < 1.0f)
            {
                timePassed += Time.deltaTime;
                if (newErrorPassed)
                {
                    timePassed = 0;
                }
                yield return null;
            }
            errorText.gameObject.SetActive(false);
            errorClearRoutine = null;
        }



        private void DisplayLoseGameUI()
        {
            mainOverlayHUD.SetEndGameText("You failed to stop the invasion.", false);
        }

        private void DisplayWinGameUI()
        {
            mainOverlayHUD.SetEndGameText("You have stopped the Invasion", true);
        }

    }
}