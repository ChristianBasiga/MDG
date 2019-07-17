using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;


namespace MDG {
    
    public class PlayerMove : MonoBehaviour
    {
        public const string WorkerType = "UnityClient";

        [SerializeField] private string horizInputName, vertInputName;
        [SerializeField] private float speed = 20;
        CharacterController controller;
        // Start is called before the first frame update


        //To be that accurate overtime and keep smooth is rough to do from scratch.
        [SerializeField] private AnimationCurve jumpFallOff;
        private float timeInAir;
        private float jumpSpeed = 5.0f;
        private bool isJumping;
        GameObject player;


        GameObject Player
        {
            get
            {
                //Not going to work cause single scene may have multiple players all with player tag.
                if (!player)
                {
                    GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Player");
                    if (gameObjects.Length > 0)
                        player = GameObject.FindGameObjectsWithTag("Player")[0];
                }
                return player;

            }
        }

        CharacterController Controller
        {
            get
            {
                if (!controller)
                {
                    controller = Player.GetComponent<CharacterController>();
                }

                return controller;
            }
        }
        //This will have reference to the reader and writer of component.
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            PlayerMovement();
            InputJump();
        }

        void PlayerMovement()
        {

            float horizInput = Input.GetAxis(horizInputName) * speed;
            float vertInput = Input.GetAxis(vertInputName) * speed;

            Vector3 forwardMovement = Player.transform.forward * vertInput;
            Vector3 rightMovement = Player.transform.right * horizInput;

            //Applies transform.transalte & scales it by delta time.
            Controller.SimpleMove(forwardMovement + rightMovement);


            //Update transform for synchro. Actually it says does it by itself. As long as 
        }

        void InputJump()
        {

            if (Input.GetKey(KeyCode.Space) && !isJumping)
            {
                StartCoroutine(PerformJump());

            }
        }

        IEnumerator PerformJump()
        {


            timeInAir = 0;
            isJumping = true;

            //To prevent clipping with camera, move camera down to 0.9, and make clipping plane 0.1 to fill up that space.
            do
            {
                float jumpForce = jumpFallOff.Evaluate(timeInAir);

                //Move doesn't applie time delta time like simple move does.
                Controller.Move(Vector3.up * jumpForce * jumpSpeed * Time.deltaTime);

                timeInAir += Time.deltaTime;
                yield return null;

            } while (Controller.collisionFlags != CollisionFlags.Below && Controller.collisionFlags != CollisionFlags.Above);

            isJumping = false;

        }
    }


}