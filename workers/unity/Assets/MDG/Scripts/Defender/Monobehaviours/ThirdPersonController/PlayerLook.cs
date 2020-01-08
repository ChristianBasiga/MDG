using Improbable.Gdk.Subscriptions;
using MDG.Common;
using MDG.Common.Interfaces;
using MDG.Common.MonoBehaviours;
using MDG.ScriptableObjects.Game;
using MdgSchema.Common;
using UnityEngine;

namespace MDG.Defender.Monobehaviours
{
    // Update this to be on body and reference camera.
    public class PlayerLook : MonoBehaviour, IProcessInput
    {
#pragma warning disable 649

        [SerializeField]
        private GameObject playerCamera;

        [SerializeField]
        private Transform target;

        [Require] EntityRotationWriter entityRotationReader;
#pragma warning restore 649

        float mouseX, mouseY;

        DefenderConfig defenderConfig;
        InputConfig inputConfig;

        // Start is called before the first frame update
        void Start()
        {
            /*GetComponent<DefenderSynchronizer>().OnEndGame += () => {
                this.enabled = false;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            };*/
            // Really just make abstract calss instead of interface at this point.
            AddToManager();
            Cursor.visible = false;
        }


        public void Init(DefenderConfig defenderConfig, InputConfig inputConfig)
        {
            this.inputConfig = inputConfig;
            this.defenderConfig = defenderConfig;
        }

        void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void UnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void CameraRotation()
        {
            if (Input.mousePosition.y > Screen.height || Input.mousePosition.y < 0 || Input.mousePosition.x > Screen.width || Input.mousePosition.x < 0)
            {
                return;
            }
            Debug.Log("Do I happen?");
            //The angles of rotation.
            mouseX += Input.GetAxis(inputConfig.XMouseMovement) * defenderConfig.MouseSensitivty * Time.deltaTime;
            mouseY -= Input.GetAxis(inputConfig.YMouseMovement) * defenderConfig.MouseSensitivty * Time.deltaTime;

            ClampYAxisRotation();

            playerCamera.transform.LookAt(target);
            target.rotation = Quaternion.Euler(mouseY, mouseX, 0);
            transform.rotation = Quaternion.Euler(0, mouseX, 0);
            entityRotationReader.SendUpdate(new EntityRotation.Update
            {
                Rotation = HelperFunctions.Vector3fFromUnityVector(transform.rotation.eulerAngles)
            });
        }

        void ClampYAxisRotation()
        {
            mouseY = Mathf.Clamp(mouseY, -20, 60);
        }

        public void AddToManager()
        {
            GetComponent<InputProcessorManager>().AddInputProcessor(this);
        }

        public void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }
            // This could be triggered after disable is issue. which undoes lock cursor.
            else if (Application.isFocused)
            {
                LockCursor();
                CameraRotation();
            }
        }

        public void Disable()
        {
            UnlockCursor();
          //  this.enabled = false;
        }

        public void Enable()
        {
            LockCursor();
           
        }
    }
}