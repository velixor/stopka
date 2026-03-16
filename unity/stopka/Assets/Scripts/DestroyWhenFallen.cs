using UnityEngine;

namespace Stopka
{
    /// <summary>
    /// Destroys the GameObject when it falls below a Y threshold.
    /// If it lands on something and stays, it remains in the scene.
    /// </summary>
    public class DestroyWhenFallen : MonoBehaviour
    {
        private const float DestroyBelowY = -10f;

        private void Update()
        {
            if (transform.position.y < DestroyBelowY)
                Destroy(gameObject);
        }
    }
}
