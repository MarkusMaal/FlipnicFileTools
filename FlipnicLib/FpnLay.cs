namespace FlipnicLib
{
    public class FpnLay
    {
        private readonly List<Layout> layouts = [];

        public FpnLay(byte[] data)
        {
            var offset = 0x40;
            while (offset <= data.Length - 0xB0)
            {
                int additionalDataLength;
                try
                {
                    additionalDataLength = BitConverter.ToInt32(data, offset + 0x58);
                }
                catch
                {
                    break;
                }

                layouts.Add(new Layout
                {
                    Label = StaticUtils.GetString([.. data.Skip(offset).Take(0x10)]),
                    SizeX = StaticUtils.GetFloat(data, offset + 0x50),
                    SizeY = StaticUtils.GetFloat(data, offset + 0x64),
                    SizeZ = StaticUtils.GetFloat(data, offset + 0x78),
                    SkewX = StaticUtils.GetFloat(data, offset + 0x70),
                    SkewY = StaticUtils.GetFloat(data, offset + 0x54),
                    SkewZ = StaticUtils.GetFloat(data, offset + 0x58),
                    PositionX = StaticUtils.GetFloat(data, offset + 0x80),
                    PositionY = StaticUtils.GetFloat(data, offset + 0x84),
                    PositionZ = StaticUtils.GetFloat(data, offset + 0x88),
                });
                if (additionalDataLength < 0x7FFF)
                {
                    offset += additionalDataLength * 0x10;
                }
                offset += 0xB0;
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
}
