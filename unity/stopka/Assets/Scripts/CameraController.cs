using UnityEngine;

namespace Stopka
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.5f;
        [SerializeField] private Vector3 offset = new Vector3(5f, 5f, 5f);
        [SerializeField] private float colorShiftSpeed = 0.01f;
        [SerializeField] private float pullbackSmoothTime = 1f;

        private float targetY;
        private float currentY;
        private float velocityY;
        private Camera cam;
        private float baseHue;
        private float pullbackTargetY;
        private bool isPullingBack;

        private void Start()
        {
            targetY = 0f;
            transform.position = offset;
            transform.LookAt(Vector3.zero);
            baseHue = Random.Range(0f, 1f);
            cam = GetComponent<Camera>();
        }

        public void SetTargetHeight(float height)
        {
            targetY = height;
        }

        public void ShowFullTower(float towerHeight)
        {
            pullbackTargetY = towerHeight / 2f;
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
            transform.position = offset;
            transform.LookAt(Vector3.zero);
        }

        private void LateUpdate()
        {
            float target = isPullingBack ? pullbackTargetY : targetY;
            currentY = Mathf.SmoothDamp(currentY, target, ref velocityY,
                isPullingBack ? pullbackSmoothTime : smoothTime);

            float extraDistance = isPullingBack ? targetY * 0.3f : 0f;
            Vector3 camOffset = offset + new Vector3(extraDistance, extraDistance, extraDistance);

            transform.position = new Vector3(camOffset.x, currentY + camOffset.y, camOffset.z);
            transform.LookAt(new Vector3(0f, currentY, 0f));

            if (cam != null)
            {
                float hue = (baseHue + targetY * colorShiftSpeed) % 1f;
                cam.backgroundColor = Color.HSVToRGB(hue, 0.3f, 0.15f);
            }
        }
    }
}
