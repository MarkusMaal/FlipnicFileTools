using System.Text;
using FlipnicLib.Formats;

namespace FlipnicScriptCompiler;

public class Gimmick : FormatBase
{
    private string Label { get; } // 0x00
    private FlipnicLib.Types.Gimmick.GimmickTypes Type { get; } // 0x20
    private byte[] Flags { get; } // 0x28
    private short[] UnkShorts  { get; } // 0x2C
    private float[] UnkFloats { get; } // 0x4C
    private int SoundEffect { get; } // 0x5C
    private short[] UnkShorts2 { get; } // 0x60
    private ControllerButtons Button { get; } // 0x6C
    private byte AnalogRange { get; } // 0x6D
    private byte[] UnkBytes { get; } // 0x6E
    private float[] LastFloats { get; set; } // 0x70
    
    public Gimmick(byte[] data)
    {
        Label = GetString(data);
        Type = (FlipnicLib.Types.Gimmick.GimmickTypes)data[0x20];
        Flags = [.. data.Skip(0x21).Take(0xB)];
        UnkShorts = new short[4];
        UnkShorts2 = new short[6];
        UnkFloats = new float[4];
        UnkBytes = new byte[2];
        LastFloats = new float[4];
        for (var i = 0; i < 4; i++)
            UnkShorts[i] = GetInt16(data, 0x2C + i * 2);
        for (var i = 0; i < 4; i++)
            UnkFloats[i] = GetFloat(data, 0x4C + i * 4);
        SoundEffect = GetInt32(data, 0x5C);
        for (var i = 0; i < 6; i++)
            UnkShorts2[i] = GetInt16(data, 0x60 + i * 2);
        Button = (ControllerButtons)data[0x6C];
        AnalogRange = data[0x6D];
        UnkBytes = [.. data.Skip(0x6E).Take(2)];
        for (var i = 0; i < 4; i++)
            LastFloats[i] = GetFloat(data, 0x70 + i * 4);
    }

    public Gimmick(string line)
    {
        var lSplit = line.Split(',');
        Label = lSplit[0];
        Type = Enum.Parse<FlipnicLib.Types.Gimmick.GimmickTypes>(lSplit[1]);
        SoundEffect = int.Parse(lSplit[2]);
        Button = Enum.Parse<ControllerButtons>(lSplit[3]);
        Flags = new byte[0xB];
        UnkShorts = new short[4];
        UnkShorts2 = new short[6];
        UnkFloats = new float[4];
        UnkBytes = new byte[2];
        LastFloats = new float[3];
        var i = 0x5;
        for (var j = 0; j < Flags.Length; j++)
        {
            Flags[j] = byte.Parse(lSplit[i]);
            i++;
        }

        for (var j = 0; j < UnkShorts.Length; j++)
        {
            UnkShorts[j] = short.Parse(lSplit[i]);
            i++;
        }

        for (var j = 0; j < UnkShorts2.Length; j++)
        {
            UnkShorts2[j] = short.Parse(lSplit[i]);
            i++;
        }

        for (var j = 0; j < UnkFloats.Length; j++)
        {
            UnkFloats[j] = float.Parse(lSplit[i]);
            i++;
        }

        i++;

        for (var j = 0; j < UnkBytes.Length; j++)
        {
            UnkBytes[j] = byte.Parse(lSplit[i - 1]);
            i++;
        }

        for (var j = 0; j < LastFloats.Length; j++)
        {
            LastFloats[j] = float.Parse(lSplit[i-1]);
            i++;
        }
    }

    public override string ToString()
    {
        return
            $"{Label},{Type},{SoundEffect},{Button},{AnalogRange},{string.Join(',', Flags)},{string.Join(',', UnkShorts)},{string.Join(',', UnkShorts2)},{string.Join(',', UnkFloats)},{string.Join(',', UnkBytes)},{string.Join(',', LastFloats)}";
    }

    public byte[] ToBytes()
    {
        var ms = new MemoryStream();
        ms.SetLength(0x80);
        ms.Write(Encoding.UTF8.GetBytes(Label));
        ms.Position = 0x20;
        ms.WriteByte((byte)Type);
        foreach (var f in Flags)
            ms.WriteByte(f);
        ms.Position = 0x2C;
        foreach (var s in UnkShorts)
            ms.Write(BitConverter.GetBytes(s));

        ms.Position = 0x4C;
        foreach (var f in UnkFloats)
            ms.Write(BitConverter.GetBytes(f));
        ms.Write(BitConverter.GetBytes(SoundEffect));
        ms.Position = 0x60;
        foreach (var s in UnkShorts2)
            ms.Write(BitConverter.GetBytes(s));
        ms.Position = 0x6C;
        ms.WriteByte((byte)Button);
        ms.WriteByte(AnalogRange);
        foreach (var b in UnkBytes)
            ms.WriteByte(b);
        foreach (var f in LastFloats)
            ms.Write(BitConverter.GetBytes(f));
        var oA = ms.ToArray();
        ms.Close();
        return oA;
    }
}