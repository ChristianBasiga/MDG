using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Unity.Entities;
using Improbable;
using Unity.Rendering;
using MDG.ScriptableObjects.Game;

namespace MDG.Invader.Monobehaviours
{
    public class CameraController : MonoBehaviour
    {
        public class Settings
        {
            public Vector2 panningBorder;
            public float panningSpeed;
            public Vector2 panningBounds;
            public float scrollSpeed;

            // Border and bounds should prob be based on window size.
            public Settings(Vector2 panningBorder, Vector2 panningBounds, float panningSpeed, float scrollSpeed)
            {
                this.panningBorder = panningBorder;
                this.panningBounds = panningBounds;
                this.panningSpeed = panningSpeed;
                this.scrollSpeed = scrollSpeed;
            }
        }
        Settings cameraSettings;
        private float minZoom;
        private float maxZoom;
        Camera viewCamera;
        private void Start()
        {
            viewCamera = GetComponent<Camera>();
            // Loading all at once prob fine, for now but ideally I load in single place, and everywhere else reache sit
            // to avoid I/O or move I/O fetches to async
            InvaderConfig invaderConfig = Resources.Load("ScriptableObjects/GameConfigs/BaseInvaderConfig") as InvaderConfig;
            invaderConfig.PanningBorder = new Vector2(Screen.width * 0.2f, Screen.height * 0.2f);
            invaderConfig.PanningBounds = new Vector2(Screen.width, Screen.height);
            Debug.Log(invaderConfig);
            Debug.Log(invaderConfig.PanningBorder);
            minZoom = invaderConfig.MinZoom;
            maxZoom = invaderConfig.MaxZoom;
            Debug.Log($"width {Screen.width} and height {Screen.height}");
            cameraSettings = new Settings(invaderConfig.PanningBorder, invaderConfig.PanningBounds, invaderConfig.CameraPanSpeed, invaderConfig.ScrollSpeed);
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
           // transform.position.y -= scroll * cameraSettings.scrollSpeed * 100.0f * Time.deltaTime;
            viewCamera.orthographicSize -= scroll * cameraSettings.scrollSpeed * 100.0f * Time.deltaTime;
            viewCamera.orthographicSize = Mathf.Clamp(viewCamera.orthographicSize, minZoom, maxZoom);
            newCameraPosition.x = Mathf.Clamp(newCameraPosition.x, -cameraSettings.panningBounds.x, cameraSettings.panningBounds.x);
            newCameraPosition.z = Mathf.Clamp(newCameraPosition.z, -cameraSettings.panningBounds.y, cameraSettings.panningBounds.y);
            transform.position = newCameraPosition;
        }
    }
}