using UnityEngine;

namespace Stopka
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.5f;
        [SerializeField] private Vector3 offset = new Vector3(5f, 5f, 5f);
        [SerializeField] private float colorShiftSpeed = 0.01f;

        private float targetY;
        private float currentY;
        private float velocityY;
        private Camera cam;
        private float baseHue;

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

        private void LateUpdate()
        {
            currentY = Mathf.SmoothDamp(currentY, targetY, ref velocityY, smoothTime);
            transform.position = new Vector3(offset.x, currentY + offset.y, offset.z);
            transform.LookAt(new Vector3(0f, currentY, 0f));
            if (cam != null)
            {
                float hue = (baseHue + targetY * colorShiftSpeed) % 1f;
                cam.backgroundColor = Color.HSVToRGB(hue, 0.3f, 0.15f);
            }
        }
    }
}
