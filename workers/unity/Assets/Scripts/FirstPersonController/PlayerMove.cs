using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG {
    
    public class PlayerMove : MonoBehaviour
    {

        public delegate void PlayerMoveHandler(Vector3 position, Vector3 rotation);
        public event PlayerMoveHandler OnPlayerMove;

        [SerializeField] private string horizInputName, vertInputName;
        [SerializeField] private float speed = 20;
        CharacterController controller;
        // Start is called before the first frame update


        //To be that accurate overtime and keep smooth is rough to do from scratch.
        [SerializeField] private AnimationCurve jumpFallOff;
        private float timeInAir;
        private float jumpSpeed = 5.0f;
        private bool isJumping;

        
        //This will have reference to the reader and writer of component.
        void Start()
        {
            controller = GetComponent<CharacterController>();
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
            transform.position += (forwardMovement + rightMovement * speed * Time.deltaTime);
            //Applies transform.transalte & scales it by delta time.
            //controller.SimpleMove(forwardMovement + rightMovement * speed);

            if (horizInput != 0 || vertInput != 0)
            {
                OnPlayerMoveHandler();
            }

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
                OnPlayerMoveHandler();
                float jumpForce = jumpFallOff.Evaluate(timeInAir);

                //Move doesn't applie time delta time like simple move does.
                controller.Move(Vector3.up * jumpForce * jumpSpeed * Time.deltaTime);
                timeInAir += Time.deltaTime;
                yield return null;

            } while (controller.collisionFlags != CollisionFlags.Below && controller.collisionFlags != CollisionFlags.Above);

            isJumping = false;

        }


        private void OnPlayerMoveHandler()
        {
            //OnPlayerMove?.Invoke(transform.position, transform.rotation.eulerAngles);
        }
    }


}