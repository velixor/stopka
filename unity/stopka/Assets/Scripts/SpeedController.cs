using UnityEngine;

namespace Stopka
{
    public enum SpeedStrategy { Sawtooth, HalfReset, Accelerating }

    public class SpeedController
    {
        private readonly GameConfig config;
        private int cycle;
        private int layerInCycle;

        public SpeedController(GameConfig config)
        {
            this.config = config;
        }

        public float GetNextSpeed()
        {
            float increment = config.speedIncrement;
            float baseSpeed = config.startSpeed;

            switch (config.speedStrategy)
            {
                case SpeedStrategy.HalfReset:
                    if (cycle > 0)
                        baseSpeed = (config.startSpeed + config.maxSpeed) / 2f;
                    break;
                case SpeedStrategy.Accelerating:
                    increment *= (1f + cycle * config.speedAcceleration);
                    break;
            }

            float speed = Mathf.Min(baseSpeed + increment * layerInCycle, config.maxSpeed);
            layerInCycle++;

            if (speed >= config.maxSpeed)
            {
                cycle++;
                layerInCycle = 0;
            }

            return speed;
        }

        public void Reset()
        {
            cycle = 0;
            layerInCycle = 0;
        }
    }
}
