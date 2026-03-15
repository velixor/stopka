using UnityEngine;

namespace Stopka
{
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Position of the top of the tower (center of the top block).</summary>
        public Vector3 TopPosition { get; private set; }

        /// <summary>Current size of the top block (X, Z).</summary>
        public Vector2 TopSize { get; private set; }

        public void Initialize()
        {
            TopPosition = Vector3.zero;
            TopSize = config.startBlockSize;
        }

        /// <summary>Update after a block is placed.</summary>
        public void PlaceBlock(Vector3 position, Vector2 size)
        {
            TopPosition = position;
            TopSize = size;
        }

        /// <summary>Get the center of the top block along a given axis.</summary>
        public float GetTopCenter(SlideAxis axis)
        {
            return axis == SlideAxis.X ? TopPosition.x : TopPosition.z;
        }

        /// <summary>Get the size of the top block along a given axis.</summary>
        public float GetTopSize(SlideAxis axis)
        {
            return axis == SlideAxis.X ? TopSize.x : TopSize.y;
        }
    }
}
