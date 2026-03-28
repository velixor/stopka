using System.Collections;
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
            cam = GetComponent<Camera>();
            baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
            ApplyInitialPosition();
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
            ApplyInitialPosition();
        }

        private void ApplyInitialPosition()
        {
            float lookAtOffset = (0.5f - config.blockScreenY) * 2f * baseOrthoSize;
            transform.position = new Vector3(offset.x, offset.y + lookAtOffset, offset.z);
            transform.LookAt(new Vector3(0f, lookAtOffset, 0f));
        }

        private Coroutine smoothResetCoroutine;

        public void SmoothResetToOrigin(float duration = 1f)
        {
            isPullingBack = false;
            if (smoothResetCoroutine != null)
                StopCoroutine(smoothResetCoroutine);
            smoothResetCoroutine = StartCoroutine(SmoothResetCoroutine(duration));
        }

        private IEnumerator SmoothResetCoroutine(float duration)
        {
            float startY = currentY;
            float startOrtho = cam != null ? cam.orthographicSize : baseOrthoSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                currentY = Mathf.Lerp(startY, 0f, t);
                targetY = 0f;
                velocityY = 0f;
                if (cam != null)
                    cam.orthographicSize = Mathf.Lerp(startOrtho, baseOrthoSize, t);
                yield return null;
            }

            currentY = 0f;
            targetY = 0f;
            velocityY = 0f;
            orthoSizeVelocity = 0f;
            if (cam != null) cam.orthographicSize = baseOrthoSize;
            smoothResetCoroutine = null;
        }

        private void LateUpdate()
        {
            float target = isPullingBack ? pullbackTargetY : targetY;
            currentY = Mathf.SmoothDamp(currentY, target, ref velocityY,
                isPullingBack ? pullbackSmoothTime : smoothTime);

            float lookAtOffset = (0.5f - config.blockScreenY) * 2f * cam.orthographicSize;
            transform.position = new Vector3(offset.x, currentY + offset.y + lookAtOffset, offset.z);
            transform.LookAt(new Vector3(0f, currentY + lookAtOffset, 0f));

            if (isPullingBack && cam != null)
            {
                cam.orthographicSize = Mathf.SmoothDamp(
                    cam.orthographicSize, targetOrthoSize,
                    ref orthoSizeVelocity, pullbackSmoothTime);
            }

        }
    }
}
