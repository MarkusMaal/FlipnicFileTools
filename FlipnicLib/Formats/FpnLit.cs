namespace FlipnicLib.Formats;

public class FpnLit() : FormatBase
{
    public List<ColorIntensity> LightMaps { get; } = [];
    
    public FpnLit(Stream dataStream) : this()
    {
        var startBytes = new byte[0x10];
        dataStream.ReadExactly(startBytes);
        var byteProduct = startBytes[0] * startBytes[1] * (long)(startBytes[2] * startBytes[3]);
        if (byteProduct != 4228250625L)
        {
            dataStream.Position = 0;
        }
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

    public byte[] GetBytes()
    {
        List<byte> data = [255, 255, 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        foreach (var lm in LightMaps)
        {
            data.AddRange(BitConverter.GetBytes(lm.Red));
            data.AddRange(BitConverter.GetBytes(lm.Green));
            data.AddRange(BitConverter.GetBytes(lm.Blue));
            data.AddRange([0, 0, 0, 0]);
        }
        return data.ToArray();
    }
    
    public override string ToString()
    {
        return ToString(false);
    }

    public class ColorIntensity
    {
        public float Red { get; init; }
        public float Green { get; init; }
        public float Blue { get; init; }
    }
}