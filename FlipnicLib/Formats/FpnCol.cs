using System.Globalization;

namespace FlipnicLib.Formats;

public class FpnCol
{

    private bool HasDefaultValue = false;
    private int GroundCount = 0;
    private int WallCount = 0;
    private float[] DefaultValues = new float[4];

    public List<ObjectDefinition> Grounds { get; set; } = [];
    public List<ObjectDefinition> Walls { get; set; } = [];

    
    public FpnCol(string filename) : this(File.OpenRead(filename)) {}


    public FpnCol(Stream stream)
    {
        stream.Seek(7, SeekOrigin.Begin);
        HasDefaultValue = stream.ReadByte() > 0;
        var counts = new byte[8];
        stream.ReadExactly(counts, 0, counts.Length);
        GroundCount = StaticUtils.GetInt32(counts, 0);
        WallCount = StaticUtils.GetInt32(counts, 4);
        var vec4 = new byte[16];
        if (HasDefaultValue)
        {
            stream.ReadExactly(vec4, 0, 16);
            DefaultValues =
            [
                StaticUtils.GetFloat(vec4, 0), StaticUtils.GetFloat(vec4, 4), StaticUtils.GetFloat(vec4, 8),
                StaticUtils.GetFloat(vec4, 12)
            ];
        }

        for (var i = 0; i < WallCount + GroundCount; i++)
        {
            var od = CreateObjectDefinition(stream);
            if (od.Label.StartsWith("GRD"))
            {
                Grounds.Add(od);
                continue;
            }
            Walls.Add(od);
        }
    }

    private static ObjectDefinition CreateObjectDefinition(Stream stream)
    {
        var sectionHeader = new byte[0x30];
        var vec4 = new byte[16];
        var f = new byte[4];
        stream.ReadExactly(sectionHeader, 0, 0x30);
        var label = StaticUtils.GetString(sectionHeader.Skip(0x10).Take(0x8).ToArray());
        var headCount =  StaticUtils.GetInt32(sectionHeader, 0x28);
        List<float[]> collFirst = [];
        List<float[]> collMain = [];
        for (var i = 0; i < headCount * 2; i++)
        {
            stream.ReadExactly(vec4, 0, vec4.Length);
            collFirst.Add([StaticUtils.GetFloat(vec4, 0), StaticUtils.GetFloat(vec4, 4),
                StaticUtils.GetFloat(vec4, 8), StaticUtils.GetFloat(vec4, 12)]);
        }
        stream.ReadExactly(f, 0, f.Length);
        var mainCount = StaticUtils.GetInt32(f, 0x00);
        stream.Seek(0xC, SeekOrigin.Current);
        for (var i = 0; i < mainCount * 2; i++)
        {
            stream.ReadExactly(vec4, 0, vec4.Length);
            collMain.Add([StaticUtils.GetFloat(vec4, 0), StaticUtils.GetFloat(vec4, 4),
                StaticUtils.GetFloat(vec4, 8), StaticUtils.GetFloat(vec4, 12)]);
        }
        return new ObjectDefinition
        {
            Label = label,
            CollFirst = collFirst,
            CollMain = collMain,
        };
    }

    public struct ObjectDefinition
    {
        public string Label { get; init; }
        public List<float[]> CollFirst { get; set; }
        public List<float[]> CollMain { get; init; }

        public override string ToString()
        {
            return Label;
        }
    }

    public override string ToString()
    {
        return ToString(StaticUtils.SimpleOutput);
    }

    public string ToString(bool asCsv)
    {
        var simpleText = $"""
                            Collision map
                            Walls: {WallCount}
                            Grounds: {GroundCount}
                            
                            """;
        string[] colHeaders = ["Label", "Base vertices", "Vertices"];
        List<string[]> rows = [];
        rows.AddRange(Grounds.Select(grd => (string[])[grd.Label, (grd.CollFirst.Count / 2).ToString(), (grd.CollMain.Count / 2).ToString()]));
        rows.AddRange(Walls.Select(wall => (string[])[wall.Label, (wall.CollFirst.Count / 2).ToString(), (wall.CollMain.Count / 2).ToString()]));
        return simpleText + StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }

    public string GenerateObj(string label)
    {
        var culture = CultureInfo.InvariantCulture;
        ObjectDefinition? od = null;
        if (label.StartsWith("GRD"))
        {
            od = Grounds.FirstOrDefault(g => g.Label == label);
        }
        if (label.StartsWith("WAL"))
        {
            od = Walls.FirstOrDefault(w => w.Label == label);
        }

        if (label == "ALL")
        {
            var result = Grounds.Aggregate("", (current, g) => current + GenerateObj(g.Label));
            return Walls.Aggregate(result, (current, w) => current + GenerateObj(w.Label));
        }

        var vertices = od == null ? "" : od.Value.CollMain.Aggregate("", (current, vertex) => current + $"v {vertex[0].ToString(culture)} {vertex[1].ToString(culture)} {vertex[2].ToString(culture)}\n");
        var faces = "";
        
        // Write face assuming every 3 vertices = 1 triangle
        for (var i = 0; i < od!.Value.CollMain.Count; i += 3)
        {
            var v1 = i + 1;
            var v2 = i + 2;
            var v3 = i + 3;

            faces += $"f {v1}/{v1} {v2}/{v2} {v3}/{v3}\n";
        }
        return vertices + faces;
    }
}