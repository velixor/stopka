using UnityEngine;

namespace Stopka
{
    public enum SlideAxis { X, Z }

    public class Block : MonoBehaviour
    {
        private SlideAxis axis;
        private float speed;
        private float slideRange;
        private float basePosition;
        private float timeAccumulator;
        private bool isMoving;

        /// <summary>Current size of this block (X and Z dimensions).</summary>
        public Vector2 Size { get; private set; }

        /// <summary>The axis this block slides along.</summary>
        public SlideAxis Axis => axis;

        public void Initialize(SlideAxis axis, float speed, float slideRange, Vector2 size, float height)
        {
            this.axis = axis;
            this.speed = speed;
            this.slideRange = slideRange;
            this.isMoving = true;
            this.timeAccumulator = 0f;
            Size = size;

            // Set scale from size
            transform.localScale = new Vector3(size.x, height, size.y);

            // Base position is the center of the block below (Y is set by spawner)
            basePosition = axis == SlideAxis.X ? transform.position.x : transform.position.z;
        }

        /// <summary>Set the base position the block oscillates around.</summary>
        public void SetBasePosition(float pos)
        {
            basePosition = pos;
        }

        private void Update()
        {
            if (!isMoving) return;

            timeAccumulator += Time.deltaTime;
            float offset = Mathf.PingPong(timeAccumulator * speed, slideRange * 2f) - slideRange;

            Vector3 pos = transform.position;
            if (axis == SlideAxis.X)
                pos.x = basePosition + offset;
            else
                pos.z = basePosition + offset;

            transform.position = pos;
        }

        /// <summary>Stop sliding. Returns the current position along the slide axis.</summary>
        public float Stop()
        {
            isMoving = false;
            return axis == SlideAxis.X ? transform.position.x : transform.position.z;
        }

        /// <summary>Resize this block after slicing (updates transform scale and position).</summary>
        public void ApplySlice(float newCenter, float newSize)
        {
            Size = axis == SlideAxis.X
                ? new Vector2(newSize, Size.y)
                : new Vector2(Size.x, newSize);

            Vector3 pos = transform.position;
            if (axis == SlideAxis.X)
                pos.x = newCenter;
            else
                pos.z = newCenter;

            transform.position = pos;
            transform.localScale = new Vector3(Size.x, transform.localScale.y, Size.y);
        }

        /// <summary>Update size for combo recovery.</summary>
        public void SetSize(Vector2 newSize)
        {
            Size = newSize;
            transform.localScale = new Vector3(newSize.x, transform.localScale.y, newSize.y);
        }
    }
}
