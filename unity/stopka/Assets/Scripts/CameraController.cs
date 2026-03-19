using UnityEngine;

namespace Stopka
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private float smoothTime = 0.5f;
        [SerializeField] private float pullbackSmoothTime = 1f;

        private Vector3 offset => config.cameraOffset;

        private float targetY;
        private float currentY;
        private float velocityY;
        private Camera cam;
        private float pullbackTargetY;
        private float targetOrthoSize;
        private float baseOrthoSize;
        private float orthoSizeVelocity;
        private bool isPullingBack;

        private void Start()
        {
            targetY = 0f;
            transform.position = offset;
            transform.LookAt(Vector3.zero);
            cam = GetComponent<Camera>();
            baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
        }

        public void SetTargetHeight(float height)
        {
            targetY = height;
        }

        public void ShowFullTower(float towerHeight)
        {
            // Fit full tower in 3/4 of screen height: visibleHeight = 2 * orthoSize
            targetOrthoSize = Mathf.Max(baseOrthoSize, towerHeight / 1.5f);
            // Position camera so tower base sits near the bottom of the screen (~15% from edge)
            pullbackTargetY = targetOrthoSize * 0.85f;
            isPullingBack = true;
        }

        public void StopPullback()
        {
            isPullingBack = false;
        }

        public void ResetToOrigin()
        {
            targetY = 0f;
            currentY = 0f;
            velocityY = 0f;
            isPullingBack = false;
            orthoSizeVelocity = 0f;
            if (cam != null) cam.orthographicSize = baseOrthoSize;
            transform.position = offset;
            transform.LookAt(Vector3.zero);
        }

        private void LateUpdate()
        {
            float target = isPullingBack ? pullbackTargetY : targetY;
            currentY = Mathf.SmoothDamp(currentY, target, ref velocityY,
                isPullingBack ? pullbackSmoothTime : smoothTime);

            transform.position = new Vector3(offset.x, currentY + offset.y, offset.z);
            transform.LookAt(new Vector3(0f, currentY, 0f));

            if (isPullingBack && cam != null)
            {
                cam.orthographicSize = Mathf.SmoothDamp(
                    cam.orthographicSize, targetOrthoSize,
                    ref orthoSizeVelocity, pullbackSmoothTime);
            }

        }
    }
}
