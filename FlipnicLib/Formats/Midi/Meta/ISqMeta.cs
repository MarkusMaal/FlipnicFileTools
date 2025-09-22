using Syroot.BinaryData;

namespace FlipnicLib.Formats.Midi.Meta;

public interface ISqMeta
{
    public void Read(BinaryStream bs);
}
