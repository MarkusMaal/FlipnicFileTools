namespace FlipnicLib.Formats;

public class FpnLay
{
    private readonly List<Layout> layouts = [];

    public FpnLay(byte[] data)
    {
        var offset = 0x30;
        while (offset < data.Length)
        {
            var count = StaticUtils.GetInt32(data, offset);
            var unknown = StaticUtils.GetInt32(data, offset + 4);
            var label = StaticUtils.GetStringAt(data, offset + 0x10);
            var test = StaticUtils.GetInt32(data, offset + 0x50);
            var additionalDataLength = StaticUtils.GetInt32(data, offset + 0x58);
            var sizeX = StaticUtils.GetFloat(data, offset + 0x60);
            var skewY = StaticUtils.GetFloat(data, offset + 0x64);
            var skewZ = StaticUtils.GetFloat(data, offset + 0x68);
            var sizeY = StaticUtils.GetFloat(data, offset + 0x74);
            var skewX = StaticUtils.GetFloat(data, offset + 0x80);
            var sizeZ = StaticUtils.GetFloat(data, offset + 0x88);
            var posX =  StaticUtils.GetFloat(data, offset + 0x90);
            var posY =  StaticUtils.GetFloat(data, offset + 0x94);
            var posZ =  StaticUtils.GetFloat(data, offset + 0x98);
            layouts.Add(new Layout
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

    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["Label", "Size", "Skew", "Position"];
        List<string[]> rows = [];
        rows.AddRange(layouts.Select(lay => (string[])[lay.Label ?? "",
            $"{StaticUtils.DotFloatString(lay.SizeX)}/{StaticUtils.DotFloatString(lay.SizeY)}/{StaticUtils.DotFloatString(lay.SizeZ)}",
            $"{StaticUtils.DotFloatString(lay.SkewX)}/{StaticUtils.DotFloatString(lay.SkewY)}/{StaticUtils.DotFloatString(lay.SkewZ)}",
            $"{StaticUtils.DotFloatString(lay.PositionX)}/{StaticUtils.DotFloatString(lay.PositionY)}/{StaticUtils.DotFloatString(lay.PositionZ)}"]));
        return StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }
    public override string ToString()
    {
        return ToString(false);
    }

    private class Layout
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