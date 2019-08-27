using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Hunter.Monobehaviours
{
    public class CameraController : MonoBehaviour
    {
        private Vector2 panningBorder;
        private int panningSpeed;
        //Will inject panning border and speed later on
        private Vector2 panningBounds;
        private float scrollSpeed;
        Camera camera;
        void Start()
        {
            panningBorder = new Vector2(Screen.width * 0.2f, Screen.height * 0.2f);
            panningSpeed = 50;
            scrollSpeed = 20;
            panningBounds = new Vector2(110, 110);
            camera = GetComponent<Camera>();
        }
        
        void Update()
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 newCameraPosition = transform.position;

            if (mousePosition.y > Screen.height || mousePosition.y < 0 || mousePosition.x > Screen.width ||  mousePosition.x < 0)
            {
                return;
            }

            if (mousePosition.y >= Screen.height - panningBorder.y)
            {
                newCameraPosition.z += panningSpeed * Time.deltaTime;
            }
            else if (mousePosition.y <= panningBorder.y)
            {
                newCameraPosition.z += -panningSpeed * Time.deltaTime;
            }

            if (mousePosition.x >= Screen.width - panningBorder.x)
            {
                newCameraPosition.x += panningSpeed * Time.deltaTime;
            }
            else if (mousePosition.x <= panningBorder.x)
            {
                newCameraPosition.x += -panningSpeed * Time.deltaTime;
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            camera.orthographicSize -= scroll * scrollSpeed * 100.0f * Time.deltaTime;
            newCameraPosition.x = Mathf.Clamp(newCameraPosition.x, -panningBounds.x, panningBounds.x);
            newCameraPosition.z = Mathf.Clamp(newCameraPosition.z, -panningBounds.y, panningBounds.y);
            transform.position = newCameraPosition;
        }

    }
}