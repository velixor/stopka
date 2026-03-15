using UnityEngine;

namespace Stopka
{
    public class BlockSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Material blockMaterial;

        private int currentLayer;

        public Block SpawnBlock(Vector3 basePosition, Vector2 currentSize)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Block_{currentLayer}";

            // Remove default collider — we'll manage physics manually for cutoff pieces
            Destroy(go.GetComponent<Collider>());

            Block block = go.AddComponent<Block>();

            SlideAxis axis = currentLayer % 2 == 0 ? SlideAxis.X : SlideAxis.Z;
            float speed = config.GetSpeedForLayer(currentLayer);
            float height = config.blockHeight;

            // Position: centered on basePosition, at correct height
            float y = basePosition.y + height;
            Vector3 spawnPos = new Vector3(basePosition.x, y, basePosition.z);
            go.transform.position = spawnPos;

            block.Initialize(axis, speed, config.slideRange, currentSize, height);

            // The block oscillates around the base position
            float center = axis == SlideAxis.X ? basePosition.x : basePosition.z;
            block.SetBasePosition(center);

            if (blockMaterial != null)
                go.GetComponent<Renderer>().material = blockMaterial;

            currentLayer++;
            return block;
        }

        public int CurrentLayer => currentLayer;

        public void ResetLayer()
        {
            currentLayer = 0;
        }
    }
}
