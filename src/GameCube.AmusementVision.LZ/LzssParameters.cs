namespace GameCube.AmusementVision.LZ
{
    internal static class LzssParameters
    {
        /// <summary>Size of the ring buffer.</summary>
        public const int N = 4096;

        /// <summary>Maximum match length for position coding. (0x0F + THRESHOLD).</summary>
        public const int F = 18;

        /// <summary>Minimum match length for position coding.</summary>
        public const int THRESHOLD = 3;

        /// <summary>Index for root of binary search trees.</summary>
        public const int NIL = N;

        /// <summary>Character used to fill the ring buffer initially.</summary>
        //private const ubyte BUFF_INIT = ' ';
        public const byte BUFF_INIT = 0; // Changed for F-Zero GX
    }
}