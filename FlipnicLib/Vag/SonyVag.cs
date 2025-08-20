namespace FlipnicLib.Vag;

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
        VagfNothing = 0,         /* Nothing*/
        VagfLoopLastBlock = 1, /* Last block to loop */
        VagfLoopRegion = 2,     /* Loop region*/
        VagfLoopEnd = 3,        /* Ending block of the loop */
        VagfLoopFirstBlock = 4,/* First block of looped data */
        VagfUnk = 5,             /* ?*/
        VagfLoopStart = 6,      /* Starting block of the loop*/
        VagfPlaybackEnd = 7     /* Playback ending position */
    };


    private const int VagSampleBytes = 14;
    private const int VagSampleNibbl = VagSampleBytes * 2;
}
