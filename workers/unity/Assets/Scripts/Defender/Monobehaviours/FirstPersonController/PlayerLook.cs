using Improbable;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using UnityEngine;
using PositionSchema = MdgSchema.Common.Position;

namespace MDG.Defender.Monobehaviours
{
    // Update this to be on body and reference camera.
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private string mouseXInput, mouseYInput;
        [SerializeField] private float mouseSensitivty;
        [SerializeField] private GameObject playerCamera;
        public Transform crossHairs;
        [Require] PositionSchema.AngularVelocityWriter angularVelocityWriter;

        private float xAxisClamp;
        private readonly float baseOffset = 360.0f;
        private readonly float max = 40.0f;

        private void Awake()
        {
            LockCursor();
            playerCamera = transform.GetChild(0).gameObject;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            CameraRotation();
        }


        void LockCursor()
        {

            Cursor.lockState = CursorLockMode.Locked;

        }

        void CameraRotation()
        {
            if (Input.mousePosition.y > Screen.height || Input.mousePosition.y < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.x < 0)
            {
                angularVelocityWriter.SendUpdate(new PositionSchema.AngularVelocity.Update
                {
                    AngularVelocity = Vector3f.Zero
                });
                return;
            }
            //The angles of rotation.
            float mouseX = Input.GetAxis(mouseXInput) * mouseSensitivty * Time.deltaTime;
            float mouseY = Input.GetAxis(mouseYInput) * mouseSensitivty * Time.deltaTime;

            xAxisClamp += mouseY;

            //Making sure we never moved more than 90 degrees in either direcion.
            if (xAxisClamp > max)
            {
                xAxisClamp = max;
                mouseY = 0;
                //As we may be slightly off due to it being floats, we may go past point we want to lock, so this forces it.
                //270 degrees in 90 degrees before full rotation on graph, ie: rotating fully up.
                ClampXAxisRotation(baseOffset - max);
            }
            else if (xAxisClamp < -max)
            {
                xAxisClamp = -max;
                mouseY = 0;
                //90 is rotating fully down, when aligned with y axis and in negative region.
                ClampXAxisRotation(max);
            }

            // Hmm Ideally don't need camera to also be synced only part that does is animation down line
            // not hard to incorporate later
            playerCamera.transform.Rotate(Vector3.left * mouseY);
            //crossHairs.transform.RotateAround(playerCamera.transform.position, Vector3.left, mouseY);

            angularVelocityWriter.SendUpdate(new PositionSchema.AngularVelocity.Update
            {
                AngularVelocity = HelperFunctions.Vector3fFromUnityVector(Vector3.up * mouseX / Time.deltaTime)
            });
        }

        void ClampXAxisRotation(float value)
        {
            Vector3 eulerAngles = playerCamera.transform.eulerAngles;
            eulerAngles.x = value;
            playerCamera.transform.eulerAngles = eulerAngles;
        }
    }
}