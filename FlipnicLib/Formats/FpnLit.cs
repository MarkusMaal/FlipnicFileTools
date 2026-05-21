namespace FlipnicLib.Formats;

public class FpnLit() : FormatBase
{
    private List<ColorIntensity> LightMaps { get; } = [];
    
    public FpnLit(Stream dataStream) : this()
    {
        dataStream.Seek(0x10, SeekOrigin.Begin);
        while (dataStream.Position < dataStream.Length)
        {
            var data = new byte[0x10];
            dataStream.ReadExactly(data, 0, data.Length);
            LightMaps.Add(new ColorIntensity
            {
                Red = GetFloat(data, 0),
                Green = GetFloat(data, 4),
                Blue = GetFloat(data, 8),
            });
        }

    }

    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["Red", "Green", "Blue"];
        List<string[]> rows = [];
        rows.AddRange(LightMaps.Select(value => (string[]) [StaticUtils.DotFloatString(value.Red), StaticUtils.DotFloatString(value.Green), StaticUtils.DotFloatString(value.Blue)]));
        return """
               Color intensities:
               
               """ + StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }
    
    public override string ToString()
    {
        return ToString(false);
    }

    private class ColorIntensity
    {
        public float Red { get; init; }
        public float Green { get; init; }
        public float Blue { get; init; }
    }
}