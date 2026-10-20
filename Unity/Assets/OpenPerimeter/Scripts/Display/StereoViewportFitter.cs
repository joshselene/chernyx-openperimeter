using UnityEngine;

namespace OpenPerimeter.Display
{
    [ExecuteAlways]
    public class StereoViewportFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private int lastWidth = 0;
        private int lastHeight = 0;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            FitToCamera();
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight)
            {
                FitToCamera();
            }
        }

        public void FitToCamera()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (targetCamera.orthographic)
            {
                float height = targetCamera.orthographicSize * 2f;
                float width = height * targetCamera.aspect;
                transform.localScale = new Vector3(width, height, 1f);
            }
            else
            {
                float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
                if (distance < 0.1f) distance = 1.0f;

                float height = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float width = height * targetCamera.aspect;
                transform.localScale = new Vector3(width, height, 1f);
            }
        }
    }
}
