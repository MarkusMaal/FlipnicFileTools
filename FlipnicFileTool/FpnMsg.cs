namespace FlipnicFileTool;

public class FpnMsg
{
    private byte[] Data;
    private List<string> Messages = [];
    private string Magic;
    public FpnMsg(string filename)
    {
        Data = File.ReadAllBytes(filename);
        Magic = StaticUtils.GetString(Data.Take(8).ToArray());
        var tocOffset = StaticUtils.GetInt32(Data, 0x08);
        var entries = StaticUtils.GetInt32(Data, 0x0C);

        for (var offset = tocOffset; offset < tocOffset + entries * 0x08; offset += 0x08)
        {
            Messages.Add(StaticUtils.GetFixedUtf16String(Data, StaticUtils.GetInt32(Data, offset), StaticUtils.GetInt16(Data, offset + 4)));
        }
    }

    public override string ToString()
    {
        return $"Magic: {Magic}\nEntries: {Messages.Count}\n" + StaticUtils.GenerateTable(["ID", "Message"],
            Messages.Select((t, i) => (string[]) [i.ToString(), t]).ToList(), 
            Messages.Select(message => message.Length + 1).Prepend(15).Max());
    }

    public string ToSimpleString()
    {
        return Messages.Aggregate("", (current, message) => current + (message + "\n"));
    }
}