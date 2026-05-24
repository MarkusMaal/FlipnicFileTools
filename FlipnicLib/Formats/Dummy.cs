using Syroot.BinaryData;

namespace FlipnicLib.Formats;
public class Dummy(Stream stream) : FormatBase
{
    private readonly long _size = stream.Length;
    private readonly string _zeroPad = "Yes" ; // the value is "Yes" if every byte is 00
    public override string ToString() => $"Dummy file\n\nPurpose: Prevent system crash when DVD laser tries to read past the game data\nZero padded: {_zeroPad}\nTotal size: {GetFilesizeString(_size)}";
}