using Improbable;
using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.ScriptableObjects.Game;
using UnityEngine;
using PositionSchema = MdgSchema.Common.Position;

namespace MDG.Defender.Monobehaviours
{
    // Update this to be on body and reference camera.
    public class PlayerLook : MonoBehaviour
    {

        [SerializeField]
        private GameObject playerBody;

        [SerializeField]
        private GameObject playerCamera;

        [SerializeField]
        private Transform target;
        [Require] PositionSchema.AngularVelocityWriter angularVelocityWriter = null;

        float mouseX, mouseY;
        private float xAxisClamp;
        private readonly float baseOffset = 360.0f;
        private readonly float maxAngle = 30.0f;

        DefenderConfig defenderConfig;
        InputConfig inputConfig;

        private void Awake()
        {
            LockCursor();
            Cursor.visible = false;
        }

        // Start is called before the first frame update
        void Start()
        {
            GetComponent<DefenderSynchronizer>().OnEndGame += () => {
                this.enabled = false;
                angularVelocityWriter.SendUpdate(new PositionSchema.AngularVelocity.Update
                {
                    AngularVelocity = Vector3f.Zero
                });
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            };
        }


        public void Init(DefenderConfig defenderConfig, InputConfig inputConfig)
        {
            this.inputConfig = inputConfig;
            this.defenderConfig = defenderConfig;
        }
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (Application.isPlaying)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                CameraRotation();
            }
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
            mouseX += Input.GetAxis(inputConfig.XMouseMovement) * defenderConfig.MouseSensitivty * Time.deltaTime;
            mouseY -= Input.GetAxis(inputConfig.YMouseMovement) * defenderConfig.MouseSensitivty * Time.deltaTime;

            ClampYAxisRotation();

            playerCamera.transform.LookAt(target);
            target.rotation = Quaternion.Euler(mouseY, mouseX, 0);
            transform.rotation = Quaternion.Euler(0, mouseX, 0);
            angularVelocityWriter.SendUpdate(new PositionSchema.AngularVelocity.Update
            {
                AngularVelocity = HelperFunctions.Vector3fFromUnityVector(Vector3.up * mouseX) *  defenderConfig.CameraMoveSpeed
            });
        }

        void ClampYAxisRotation()
        {
            mouseY = Mathf.Clamp(mouseY, -20, 60);
        }
    }
}