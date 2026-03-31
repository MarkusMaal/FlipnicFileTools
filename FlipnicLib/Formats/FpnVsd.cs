namespace FlipnicLib.Formats;

public class FpnVsd() : FormatBase
{
    private int Count { get; }
    private List<VibrationValue[]> Sections { get; } = [];
    
    public FpnVsd(Stream dataStream) : this()
    {
        var data = new byte[dataStream.Length];
        dataStream.ReadExactly(data, 0, data.Length);
        Count = GetInt32(data, 0);
        var offset = 0x10;
        for (var i = 0; i < Count; i++)
        {
            var valueCount = GetInt32(data, offset);
            var valueCount2 = GetInt32(data, offset+4);
            List<VibrationValue> sectionsValues = [];
            for (var j = 0; j < valueCount + valueCount2; j++)
            {
                var valueOffset = offset + 0x10 + j * 0x10;
                sectionsValues.Add(new VibrationValue(GetInt32(data, valueOffset), GetFloat(data, valueOffset + 4)));
            }
            Sections.Add(sectionsValues.ToArray());
            offset += 0x10 * (valueCount + valueCount2) + 0x10;
        }
    }

    public string ToString(bool asCsv)
    {
        var o = "";
        var i = 0;
        string[] colHeaders = ["Flag", "Value"];
        List<string[]> rows = [];
        foreach (var section in Sections)
        {
            o += $"\nSection {++i}\n";
            rows.Clear();
            rows.AddRange(section.Select(value => (string[]) [value.Flag.ToString(), DotFloatString(value.Strength)]));
            o += StaticUtils.GenerateTable(colHeaders, rows, asCsv);
        }
        return o;
    }
    public override string ToString() => ToString(false);

    private class VibrationValue(int flag, float strength)
    {
        public int Flag { get; } = flag;
        public float Strength { get; } = strength;
    }
}