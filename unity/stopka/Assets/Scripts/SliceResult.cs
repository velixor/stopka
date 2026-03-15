namespace Stopka
{
    public struct SliceResult
    {
        /// <summary>True if the blocks overlap at all.</summary>
        public bool HasOverlap;

        /// <summary>True if placement is within perfect tolerance.</summary>
        public bool IsPerfect;

        // --- Remaining block (the overlap region) ---
        /// <summary>Center position of the remaining block along the slide axis.</summary>
        public float NewCenter;
        /// <summary>Size of the remaining block along the slide axis.</summary>
        public float NewSize;

        // --- Cutoff piece (the overhang that falls away) ---
        /// <summary>Center position of the cut piece along the slide axis.</summary>
        public float CutCenter;
        /// <summary>Size of the cut piece along the slide axis.</summary>
        public float CutSize;
    }
}
