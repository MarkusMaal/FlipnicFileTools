namespace FlipnicLib.Formats;

public class FpnLay
{
    public List<Layout> Layouts { get; set; } = [];

    private readonly byte[] _data;

    public FpnLay(byte[] inputData)
    {
        _data = inputData;
        var offset = 0x30;
        while (offset < _data.Length)
        {
            var count = StaticUtils.GetInt32(_data, offset);
            var unknown = StaticUtils.GetInt32(_data, offset + 4);
            var label = StaticUtils.GetStringAt(_data, offset + 0x10);
            var test = StaticUtils.GetInt32(_data, offset + 0x50);
            var additionalDataLength = StaticUtils.GetInt32(_data, offset + 0x58);
            var sizeX = StaticUtils.GetFloat(_data, offset + 0x60);
            var skewY = StaticUtils.GetFloat(_data, offset + 0x64);
            var skewZ = StaticUtils.GetFloat(_data, offset + 0x68);
            var sizeY = StaticUtils.GetFloat(_data, offset + 0x74);
            var skewX = StaticUtils.GetFloat(_data, offset + 0x80);
            var sizeZ = StaticUtils.GetFloat(_data, offset + 0x88);
            var posX =  StaticUtils.GetFloat(_data, offset + 0x90);
            var posY =  StaticUtils.GetFloat(_data, offset + 0x94);
            var posZ =  StaticUtils.GetFloat(_data, offset + 0x98);
            Layouts.Add(new Layout
            {
                Label = label,
                PositionX = posX,
                PositionY = posY,
                PositionZ = posZ,
                SizeX = sizeX,
                SizeY = sizeY,
                SizeZ = sizeZ,
                SkewX = skewX,
                SkewY = skewY,
                SkewZ = skewZ,
            });
            offset += 0xB0 + (additionalDataLength < 32768 ? 0x10 * additionalDataLength : 0);
        }
    }

    public byte[] CommitChanges()
    {
        var output = new byte[_data.Length];
        Array.Copy(_data, output, _data.Length);
        
        var offset = 0x30;
        var idx = 0;
        while (offset < _data.Length)
        {
            var layout = Layouts[idx];
            foreach (var (i, ch) in (layout.Label ?? "").Index())
            {
                output[offset + i + 0x10] = (byte)ch;
            }
            var additionalDataLength = StaticUtils.GetInt32(_data, offset + 0x58); // this is read only for identifying the next offset
            StaticUtils.WriteByteArray(output, offset + 0x60, BitConverter.GetBytes(layout.SizeX));
            StaticUtils.WriteByteArray(output, offset + 0x64, BitConverter.GetBytes(layout.SkewY));
            StaticUtils.WriteByteArray(output, offset + 0x68, BitConverter.GetBytes(layout.SkewZ));
            StaticUtils.WriteByteArray(output, offset + 0x74, BitConverter.GetBytes(layout.SizeY));
            StaticUtils.WriteByteArray(output, offset + 0x80, BitConverter.GetBytes(layout.SkewX));
            StaticUtils.WriteByteArray(output, offset + 0x88, BitConverter.GetBytes(layout.SizeZ));
            StaticUtils.WriteByteArray(output, offset + 0x90, BitConverter.GetBytes(layout.PositionX));
            StaticUtils.WriteByteArray(output, offset + 0x94, BitConverter.GetBytes(layout.PositionY));
            StaticUtils.WriteByteArray(output, offset + 0x98, BitConverter.GetBytes(layout.PositionZ));
            offset += 0xB0 + (additionalDataLength < 32768 ? 0x10 * additionalDataLength : 0);
            idx++;
        }
        return output;
    }

    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["Label", "Size", "Skew", "Position"];
        List<string[]> rows = [];
        rows.AddRange(Layouts.Select(lay => (string[])[lay.Label ?? "",
            $"{StaticUtils.DotFloatString(lay.SizeX)}/{StaticUtils.DotFloatString(lay.SizeY)}/{StaticUtils.DotFloatString(lay.SizeZ)}",
            $"{StaticUtils.DotFloatString(lay.SkewX)}/{StaticUtils.DotFloatString(lay.SkewY)}/{StaticUtils.DotFloatString(lay.SkewZ)}",
            $"{StaticUtils.DotFloatString(lay.PositionX)}/{StaticUtils.DotFloatString(lay.PositionY)}/{StaticUtils.DotFloatString(lay.PositionZ)}"]));
        return StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }
    public override string ToString()
    {
        return ToString(false);
    }

    public class Layout
    {
        public string? Label { get; init; }

        public float SizeX { get; init; }
        public float SizeY { get; init; }
        public float SizeZ { get; init; }

        public float SkewX { get; init; }
        public float SkewY { get; init; }
        public float SkewZ { get; init; }

        public float PositionX { get; init; }
        public float PositionY { get; init; }
        public float PositionZ { get; init; }
    }
}