namespace FlipnicFileTool.Vag;

public static partial class SonyVag
{
    private struct VagChunk
    {
        public sbyte Shift;
        public sbyte Predict; /* swy: reversed nibbles due to little-endian */
        public byte Flags;
        public byte[] Sample;
    };

    public enum VagFlag
    {
        VagfLoopStart = 6,      /* Starting block of the loop*/
        VagfPlaybackEnd = 7     /* Playback ending position */
    };

    private const int VagSampleBytes = 14;
    private const int VagSampleNibbl = VagSampleBytes * 2;
}
