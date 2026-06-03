using FlipnicLib.Formats;

namespace FlipnicLib.Types;


public class MenuElement(byte[] data, string sectionLabel) : FormatBase
{
    public string Texture { get; set; } = GetString(data.Take(0x30).ToArray());

    public bool BgItem { get; set; } = data[0x51] > 0;

    public int PosX { get; set; } = GetInt32(data, 0x40);
    public int PosY { get; set; } =  GetInt32(data, 0x44);

    public int Width { get; set; } = GetInt32(data, 0x48);
    public int Height { get; set; } =  GetInt32(data, 0x4C);

    public int Dipth { get; set; } = GetInt32(data, 0x54);
    public int Blend { get; set; } = GetInt32(data, 0x58);
    public int Index { get; set; } = GetInt32(data, 0x5C);
        
    public override string ToString()
    {
        return $"{sectionLabel} | {Texture} ({Index})";
    }
}