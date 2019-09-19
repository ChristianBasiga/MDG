using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Unity.Entities;
using Improbable;
using Unity.Rendering;

namespace MDG.Hunter.Monobehaviours
{
    public class CameraController : MonoBehaviour
    {
        public class Settings
        {
            public Vector2 panningBorder;
            public float panningSpeed;
            public Vector2 panningBounds;
            public float scrollSpeed;

            public Settings(Vector2 panningBorder, Vector2 panningBounds, float panningSpeed, float scrollSpeed)
            {
                this.panningBorder = panningBorder;
                this.panningBounds = panningBounds;
                this.panningSpeed = panningSpeed;
                this.scrollSpeed = scrollSpeed;
            }
        }
        Settings cameraSettings;
        private readonly float minZoom = 20;
        private readonly float maxZoom = 120;
        [SerializeField] private float worldWidth;
        [SerializeField] private float worldHeight;
        new Camera camera;
        //This way caninject new camera settings as needed.
        [Inject]
        public void Initialize(Settings cameraSettings)
        {
            this.cameraSettings = cameraSettings;
        }

        public void SetCameraSettings()
        {
            // Prob will instead listen for event, instead of UI having reference to this, it will subscribe at start then let callback handle it.
        }
       
        private void Start()
        {
            camera = GetComponent<Camera>();
            if (cameraSettings == null)
            {
                cameraSettings = new Settings(new Vector2(Screen.width * 0.2f, Screen.height * 0.2f),
                new Vector2(worldWidth, worldHeight),
                50.0f,
                50.0f);
            }
        }
        void Update()
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 newCameraPosition = transform.position;
            if (mousePosition.y > Screen.height || mousePosition.y < 0 || mousePosition.x > Screen.width ||  mousePosition.x < 0)
            {
                return;
            }
            if (mousePosition.y >= Screen.height - cameraSettings.panningBorder.y)
            {
                newCameraPosition.z += cameraSettings.panningSpeed * Time.deltaTime;
            }
            else if (mousePosition.y <= cameraSettings.panningBorder.y)
            {
                newCameraPosition.z += -cameraSettings.panningSpeed * Time.deltaTime;
            }
            if (mousePosition.x >= Screen.width - cameraSettings.panningBorder.x)
            {
                newCameraPosition.x += cameraSettings.panningSpeed * Time.deltaTime;
            }
            else if (mousePosition.x <= cameraSettings.panningBorder.x)
            {
                newCameraPosition.x += -cameraSettings.panningSpeed * Time.deltaTime;
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            camera.orthographicSize -= scroll * cameraSettings.scrollSpeed * 100.0f * Time.deltaTime;
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, minZoom, maxZoom);
            newCameraPosition.x = Mathf.Clamp(newCameraPosition.x, -cameraSettings.panningBounds.x, cameraSettings.panningBounds.x);
            newCameraPosition.z = Mathf.Clamp(newCameraPosition.z, -cameraSettings.panningBounds.y, cameraSettings.panningBounds.y);
            transform.position = newCameraPosition;
        }
    }
}