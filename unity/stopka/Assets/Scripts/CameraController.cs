using UnityEngine;

namespace Stopka
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float followSpeed = 3f;
        [SerializeField] private Vector3 offset = new Vector3(5f, 5f, 5f);

        private float targetY;

        private void Start()
        {
            targetY = 0f;
            transform.position = offset;
            transform.LookAt(Vector3.zero);
        }

        public void SetTargetHeight(float height)
        {
            targetY = height;
        }

        private void LateUpdate()
        {
            Vector3 target = new Vector3(offset.x, targetY + offset.y, offset.z);
            transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
            transform.LookAt(new Vector3(0f, targetY, 0f));
        }
    }
}
