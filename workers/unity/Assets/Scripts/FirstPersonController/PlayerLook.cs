using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MDG
{
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private string mouseXInput, mouseYInput;
        [SerializeField] private float mouseSensitivty;
        [SerializeField] private PlayerMove playerBody;

        private float xAxisClamp;
        private readonly float max = 90.0f;

        private void Awake()
        {
            LockCursor();
            playerBody = transform.parent.GetComponent<PlayerMove>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // Only do camera rotation if out of screen view.
            // I mean I'm wasitnig time doing this, when I can get a much better done asset.
            // Of all things to reinvent, this ain't it.
            if (Input.mousePosition.x < Screen.width && Input.mousePosition.x > 0 && Input.mousePosition.y < Screen.height && Input.mousePosition.y > 0)
            {
                CameraRotation();
            }
        }


        void LockCursor()
        {

            Cursor.lockState = CursorLockMode.Locked;

        }

        void CameraRotation()
        {

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
                ClampXAxisRotation(270.0f);
            }
            else if (xAxisClamp < -max)
            {
                xAxisClamp = -max;
                mouseY = 0;
                //90 is rotating fully down, when aligned with y axis and in negative region.
                ClampXAxisRotation(90.0f);
            }

            //transform.right because rotating x axis moves camera along y.
            //Changed to absolute left axis, as what is considered transform.right changes when rotated.
            transform.Rotate(Vector3.left * mouseY);

            //Moving body, not just camera so that when we do movement forward and all other directions are always the what we expect.
            //and since camera is child, it rotates accordingly.
            playerBody.transform.Rotate(Vector3.up * mouseX);

        }

        void ClampXAxisRotation(float value)
        {
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x = value;
            transform.eulerAngles = eulerAngles;
        }
    }
}