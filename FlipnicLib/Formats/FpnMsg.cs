using System.Text;

namespace FlipnicLib.Formats;

public class FpnMsg
{
    private readonly List<string> _messages = [];
    private readonly string _magic;

    public string[] Messages
    {
        get => [.. _messages];
        set
        {
            _messages.Clear();
            _messages.AddRange(value);
        }
    }

    public FpnMsg(string filename) : this(File.OpenRead(filename)) {}
    
    public FpnMsg(Stream stream)
    {
        var data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        _magic = StaticUtils.GetString(data.Take(8).ToArray());
        var tocOffset = StaticUtils.GetInt32(data, 0x08);
        var entries = StaticUtils.GetInt32(data, 0x0C);

        for (var offset = tocOffset; offset < tocOffset + entries * 0x08; offset += 0x08)
        {
            _messages.Add(StaticUtils.GetFixedUtf16String(data, StaticUtils.GetInt32(data, offset), StaticUtils.GetInt16(data, offset + 4)));
        }
    }

    public FpnMsg()
    {
        _magic = "FpnMsg00";
    }

    /// <summary>
    /// Gets the message at the index specified by id
    /// </summary>
    public string GetMessageById(int id)
    {
        return id == -1 ? "MASTER" : _messages[id];
    }

    public string ToString(bool asCsv)
    {
        return $"Magic: {_magic}\nEntries: {_messages.Count}\n" + StaticUtils.GenerateTable(["ID", "Message"],
            _messages.Select((t, i) => (string[]) [i.ToString(), t]).ToList(), asCsv);   
    }
    
    public override string ToString()
    {
        return ToString(false);
    }

    /// <summary>
    /// Show messages without generating a table
    /// </summary>
    public string ToSimpleString()
    {
        return _messages.Aggregate("", (current, message) => current + (message + "\n"));
    }

    public string[] ToArray()
    {
        return _messages.ToArray();
    }

    /// <summary>
    /// Get the raw .MSG data from this object
    /// </summary>
    public byte[] GetData()
    {
        List<byte> data = [];
        data.AddRange(Encoding.UTF8.GetBytes(_magic));
        data.AddRange([0x18, 0, 0, 0]);
        data.AddRange(BitConverter.GetBytes(_messages.Count));
        data.AddRange([0,0,0,0,0,0,0,0]);
        var offset = 0x18 + 0x8 * _messages.Count;
        foreach (var message in _messages)
        {
            data.AddRange(BitConverter.GetBytes(offset));
            data.AddRange(BitConverter.GetBytes((short)(message.Length*2)));
            data.Add(0x30);
            data.Add(0x30);
            offset += (message.Length + 1) * 2;
        }

        foreach (var message in _messages)
        {
            data.AddRange(Encoding.Unicode.GetBytes(message));
            data.Add(0x00);
            data.Add(0x00);
        }
        return [.. data];
    }
}