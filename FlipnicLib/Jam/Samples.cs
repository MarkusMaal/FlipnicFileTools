using FlipnicLib.Midi;
using FlipnicLib.Vag;
using Syroot.BinaryData;

namespace FlipnicLib.Jam;

public class Samples
{
    public List<byte[]> RawSamples { get; set; } = [];
    public List<uint> LoopStarts { get; set; } = [];
    public List<uint> LoopEnds { get; set; } = [];
    public List<int> Lengths { get; set; } = [];

    public Samples(Stream s)
    {
        while (s.Position < s.Length)
        {
            var vag = GetVag(s, out var loopStart, out var loopEnd);
            RawSamples.Add(vag);
            LoopStarts.Add(loopStart);
            LoopEnds.Add(loopEnd);
            Lengths.Add(vag.Length);
        }
    }

    private byte[] GetVag(Stream bs, out uint loopStart, out uint loopEnd)
    {
        loopStart = 0;
        loopEnd = 0;
        // Size of vag is not provided, we must find it using vag flags
        long basePos = bs.Position;
        long absoluteSsaOffset = basePos;
        bs.Position = absoluteSsaOffset;

        uint lastSampleIndex = 0;
        while (bs.Position < bs.Length)
        {
            byte decodingCoef = bs.Read1Byte();
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

        bs.Position = absoluteSsaOffset;
        try
        {
            return bs.ReadBytes(0x10 * (int)(lastSampleIndex + 1));
        }
        catch (EndOfStreamException)
        {
            return [];
        }
    }
}