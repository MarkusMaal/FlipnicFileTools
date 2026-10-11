using System.Text;

namespace FlipnicScriptCompiler;

public readonly struct Event(byte[] data)
{
    public int Magic { get; } = BitConverter.ToInt32([.. data.Take(4)], 0);
    public string Label { get; } = Encoding.UTF8.GetString([..data.Skip(4).Take(0x20)]); 
    public int Arg1 { get; } = BitConverter.ToInt32([.. data.Skip(0x24).Take(4)]);
    public int Arg2 { get; } = BitConverter.ToInt32([.. data.Skip(0x28).Take(4)]);
    public int Arg3 { get; } = BitConverter.ToInt32([.. data.Skip(0x2C).Take(4)]);
    public int Arg4 { get; } = BitConverter.ToInt32([.. data.Skip(0x30).Take(4)]);
    public int Arg5 { get; } = BitConverter.ToInt32([.. data.Skip(0x34).Take(4)]);
    public int Arg6 { get; } = BitConverter.ToInt32([.. data.Skip(0x38).Take(4)]);
    public int Arg7 { get; } = BitConverter.ToInt32([.. data.Skip(0x3C).Take(4)]);
}