using Syroot.BinaryData;
using SonyVag = FlipnicLib.Formats.Vag.SonyVag;

namespace FlipnicLib.Formats.Jam;

public class Samples
{
    public List<byte[]> RawSamples { get; set; } = [];
    public List<uint> LoopStarts { get; set; } = [];
    public List<uint> LoopEnds { get; set; } = [];
    public List<int> Lengths { get; set; } = [];

    public Samples(Stream s)
    {
        s.Position = 0;
        while (s.Position < s.Length)
        {
            var vag = GetVag(s, out var loopStart, out var loopEnd);
            RawSamples.Add(vag);
            LoopStarts.Add(loopStart);
            LoopEnds.Add(loopEnd);
            Lengths.Add(vag.Length);
        }
    }

    private static byte[] GetVag(Stream bs, out uint loopStart, out uint loopEnd)
    {
        loopStart = 0;
        loopEnd = 0;
        // Size of vag is not provided, we must find it using vag flags
        var basePos = bs.Position;
        bs.Position = basePos;

        uint lastSampleIndex = 0;
        while (bs.Position < bs.Length)
        {
            var decodingCoef = bs.Read1Byte();
            var flag = (SonyVag.VagFlag)bs.Read1Byte();

            if (flag == SonyVag.VagFlag.VagfLoopStart)
                loopStart = lastSampleIndex;

            if (flag == SonyVag.VagFlag.VagfLoopLastBlock || flag == SonyVag.VagFlag.VagfLoopEnd)
            {
                if (flag == SonyVag.VagFlag.VagfLoopEnd)
                    loopEnd = lastSampleIndex;

                break;
            }
                

            lastSampleIndex++;

            bs.Position += 0x0E;
        }

        bs.Position = basePos;
        return bs.Position + 0x10 * (int)(lastSampleIndex + 1) > bs.Length ? bs.ReadBytes((int)(bs.Length - bs.Position)) : bs.ReadBytes(0x10 * (int)(lastSampleIndex + 1));
    }
}