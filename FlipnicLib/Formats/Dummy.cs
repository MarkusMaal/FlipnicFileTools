using Syroot.BinaryData;

namespace FlipnicLib.Formats;
public readonly struct Dummy(Stream stream)
{
    private readonly long _size = stream.Length;
    private readonly string _zeroPad = stream.ReadBytes((int)stream.Length).Sum(b => b) == 0 ? "Yes" : "No"; // the value is "Yes" if every byte is 00
    public override string ToString() => $"Dummy file\n\nPurpose: Prevent system crash when DVD laser tries to read past the game data\nZero padded: {_zeroPad}\nTotal size: {StaticUtils.GetFilesizeString(_size)}";
}