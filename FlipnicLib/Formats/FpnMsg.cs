namespace FlipnicLib.Formats;

public class FpnMsg
{
    private readonly List<string> _messages = [];
    private readonly string _magic;

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

    public string ToSimpleString()
    {
        return _messages.Aggregate("", (current, message) => current + (message + "\n"));
    }

    public string[] ToArray()
    {
        return _messages.ToArray();
    }
}