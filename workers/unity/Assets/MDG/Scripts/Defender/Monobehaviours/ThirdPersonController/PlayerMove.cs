using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using MDG.ScriptableObjects.Game;
using UnityEngine;
using PositionSchema = MdgSchema.Common.Position;
namespace MDG.Defender.Monobehaviours {
    
    public class PlayerMove : MonoBehaviour, IProcessInput
    {
        public delegate void PlayerMoveHandler(Vector3 position, Vector3 rotation);
        public event PlayerMoveHandler OnPlayerMove;

#pragma warning disable 649

        [Require] PositionSchema.LinearVelocityWriter linearVelocityWriter;
        [SerializeField] private AnimationCurve jumpFallOff;

#pragma warning restore 649
        private InputConfig inputConfig;
        private DefenderConfig defenderConfig;
        /*private float timeInAir;
        private float jumpSpeed = 5.0f;
        private bool isJumping;
        */

        //This will have reference to the reader and writer of component.
        void Start()
        {

            AddToManager();
            /*
            GetComponent<DefenderSynchronizer>().OnEndGame += () => {
                this.enabled = false;
            };*/

            defenderConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseDefenderConfig") as DefenderConfig;
            // Oh I'm wylin, InputCommands has to take this.
            inputConfig = Resources.Load("ScriptableObjects/GameConfigs/MouseKeyInputConfig") as InputConfig;
            GetComponent<PlayerLook>().Init(defenderConfig, inputConfig);
            GetComponent<LoadoutSelector>().Init(inputConfig);
            //this.gameObject.SetActive(alse);
        }

        public void Init(DefenderConfig defenderConfig, InputConfig inputConfig)
        {
            this.defenderConfig = defenderConfig;
            this.inputConfig = inputConfig;
        }

        void PlayerMovement()
        {

            float horizInput = Input.GetAxis(inputConfig.HorizontalAxis);
            float vertInput = Input.GetAxis(inputConfig.VerticalAxis);
            Vector3 forwardMovement = transform.forward * vertInput;
            Vector3 rightMovement = transform.right * horizInput;

            // Hmm, it does apply velocity, but tbh, it dies out literally right after sooo lol.
            // Well this is true though if send update every frame cause axis would be 0.
            // Need to figure out how apply this.
            linearVelocityWriter.SendUpdate(new PositionSchema.LinearVelocity.Update
            {
                Velocity = HelperFunctions.Vector3fFromUnityVector(forwardMovement + rightMovement)
            });

            if (horizInput != 0 || vertInput != 0)
            {
                OnPlayerMoveHandler();
            }

            //Update transform for synchro. Actually it says does it by itself. As long as 
        }

        void InputJump()
        {

           
        }

        private void OnPlayerMoveHandler()
        {
            OnPlayerMove?.Invoke(transform.position, transform.rotation.eulerAngles);
        }

        public void AddToManager()
        {
            GetComponent<InputProcessorManager>().AddInputProcessor(this);
        }

        public void ProcessInput()
        {
            PlayerMovement();
            InputJump();
        }
    }
}