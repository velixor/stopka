using UnityEngine;

namespace Stopka
{
    /// <summary>
    /// Animates a cutoff piece: slides outward from the tower and falls with gravity.
    /// No Rigidbody — fully kinematic for a clean, predictable look.
    /// </summary>
    public class CutoffPiece : MonoBehaviour
    {
        private Vector3 velocity;
        private float lifetime;
        private float elapsed;
        private Renderer cachedRenderer;
        private Color startColor;

        private const float Gravity = 12f;
        private const float FadeStart = 0.5f;

        public void Initialize(Vector3 slideDirection, float slideSpeed, float life = 2.5f)
        {
            // Slide outward + slight downward
            velocity = slideDirection * slideSpeed;
            lifetime = life;
            elapsed = 0f;

            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
            {
                startColor = cachedRenderer.material.GetColor("_BaseColor");
                // Enable transparency for fade
                var mat = cachedRenderer.material;
                mat.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
                mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Apply gravity
            velocity.y -= Gravity * Time.deltaTime;

            // Move
            transform.position += velocity * Time.deltaTime;

            // Slight tilt in slide direction for visual polish
            transform.Rotate(velocity.normalized * 30f * Time.deltaTime, Space.World);

            // Fade out after FadeStart
            if (elapsed > FadeStart && cachedRenderer != null)
            {
                float alpha = 1f - (elapsed - FadeStart) / (lifetime - FadeStart);
                var c = startColor;
                c.a = Mathf.Max(0f, alpha);
                cachedRenderer.material.SetColor("_BaseColor", c);
            }
        }
    }
}
