using UnityEngine;

namespace Stopka
{
    public static class BlockSlicer
    {
        public static SliceResult Slice(
            float prevCenter, float prevSize,
            float currCenter, float currSize,
            float perfectTolerance)
        {
            float prevMin = prevCenter - prevSize / 2f;
            float prevMax = prevCenter + prevSize / 2f;
            float currMin = currCenter - currSize / 2f;
            float currMax = currCenter + currSize / 2f;

            float overlapMin = Mathf.Max(prevMin, currMin);
            float overlapMax = Mathf.Min(prevMax, currMax);
            float overlapSize = overlapMax - overlapMin;

            if (overlapSize <= 0f)
            {
                return new SliceResult { HasOverlap = false };
            }

            float offset = Mathf.Abs(currCenter - prevCenter);
            if (offset <= perfectTolerance)
            {
                return new SliceResult
                {
                    HasOverlap = true,
                    IsPerfect = true,
                    NewCenter = prevCenter,
                    NewSize = currSize
                };
            }

            float cutMin, cutMax;
            if (currCenter > prevCenter)
            {
                cutMin = overlapMax;
                cutMax = currMax;
            }
            else
            {
                cutMin = currMin;
                cutMax = overlapMin;
            }

            return new SliceResult
            {
                HasOverlap = true,
                IsPerfect = false,
                NewCenter = (overlapMin + overlapMax) / 2f,
                NewSize = overlapSize,
                CutCenter = (cutMin + cutMax) / 2f,
                CutSize = cutMax - cutMin
            };
        }
    }
}
