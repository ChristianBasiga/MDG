using Improbable.Gdk.Subscriptions;
using MDG.Common;
using UnityEngine;
using PositionSchema = MdgSchema.Common.Position;
namespace MDG.Defender.Monobehaviours {
    
    public class PlayerMove : MonoBehaviour
    {
        public delegate void PlayerMoveHandler(Vector3 position, Vector3 rotation);
        public event PlayerMoveHandler OnPlayerMove;

        [SerializeField] private string horizInputName = "Horizontal", vertInputName = "Vertical";
        [Require] PositionSchema.LinearVelocityWriter linearVelocityWriter = null;
        // Start is called before the first frame update

        [SerializeField]
        public int speed = 100;

        //To be that accurate overtime and keep smooth is rough to do from scratch.
        [SerializeField] private AnimationCurve jumpFallOff;

        /*private float timeInAir;
        private float jumpSpeed = 5.0f;
        private bool isJumping;
        */
        
        //This will have reference to the reader and writer of component.
        void Start()
        {
            GetComponent<DefenderSynchronizer>().OnEndGame += () => {
                this.enabled = false;
            };
        }


        // Update is called once per frame
        void Update()
        {
            PlayerMovement();
            InputJump();
        }

        void PlayerMovement()
        {

            float horizInput = Input.GetAxis(horizInputName);
            float vertInput = Input.GetAxis(vertInputName);
            Vector3 forwardMovement = transform.forward * vertInput;
            Vector3 rightMovement = transform.right * horizInput;

            // Hmm, it does apply velocity, but tbh, it dies out literally right after sooo lol.
            // Well this is true though if send update every frame cause axis would be 0.
            // Need to figure out how apply this.
            linearVelocityWriter.SendUpdate(new PositionSchema.LinearVelocity.Update
            {
                Velocity = HelperFunctions.Vector3fFromUnityVector(forwardMovement + rightMovement) * speed
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
    }
}