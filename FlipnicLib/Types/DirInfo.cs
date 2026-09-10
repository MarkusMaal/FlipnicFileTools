
using System.Text.Json.Serialization;

namespace FlipnicLib.Types;

public class DirInfo
{
    public DirEntry[] Entries { get; set; } = [];

    public void AppendEntry(string dirName)
    {
        Entries = [.. Entries.Append(new DirEntry()
        {
            Directory = dirName,
            LargeBuffers = []
        })];
    }

    public void AppendLb(string fullName)
    {
        var dir = fullName.Split('\\')[0] + "\\";
        var file = fullName.Split('\\')[1];
        if (Entries.All(p => p.Directory != dir)) return;
        var entryMatch = Entries.First(p => p.Directory == dir);
        entryMatch.LargeBuffers = [.. entryMatch.LargeBuffers.Append(file)];
    }

    public class DirEntry
    {
        public required string Directory { get; init; }
        public string[] LargeBuffers { get; set; } = [];
    }
}
    
[JsonSerializable(typeof(DirInfo))]
[JsonSerializable(typeof(DirInfo.DirEntry))]
[JsonSourceGenerationOptions(WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
public partial class DirInfoGenerationContext : JsonSerializerContext
{
}