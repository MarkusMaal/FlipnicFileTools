using Syroot.BinaryData;

namespace FlipnicLib.Formats.Midi.Meta;

public class SqSetTempoEvent : ISqMeta
{
    public uint UsecPerQuarterNote { get; set; }

    public void Read(BinaryStream bs)
    {
        UsecPerQuarterNote = (uint)(bs.ReadByte() << 16 | bs.Read1Byte() << 8 | bs.Read1Byte());
    }
}
