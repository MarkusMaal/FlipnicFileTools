namespace FlipnicLib.Formats;

public class FpnMlb
{
    public Dictionary<string, MenuElement[]> Sections { get; set; } = [];
    
    public FpnMlb(byte[] data)
    {
        var SectionCount = StaticUtils.GetInt32(data, 0);
        var offset = 0x10;
        var idx = 0;
        while (idx < SectionCount)
        {
            var SectionLabel = StaticUtils.GetString(data.Skip(offset).Take(0x20).ToArray());
            var ElementCount = StaticUtils.GetInt32(data, offset+0x24);
            List<MenuElement> elements = [];
            for (var i = 0; i < ElementCount; i++)
            {
                elements.Add(new MenuElement(data.Skip(offset+0x30+(i*0x60)).Take(0x60).ToArray()));
            }
            Sections.Add(SectionLabel, elements.ToArray());
            offset += 0x30 + ElementCount * 0x60;
            idx++;
        }
    }

    public string ToString(bool asCsv)
    {
        string[] colHeaders = ["Section", "Index", "Texture", "Position", "Dimensions"];
        List<string[]> rows = [];
        foreach (var (sectLabel, value) in Sections)
        {
            rows.AddRange(value.Select(elem => (string[]) [sectLabel, elem.Index.ToString(), elem.Texture, $"{elem.PosX}x{elem.PosY}", $"{elem.Width}x{elem.Height}"]));
        }
        return StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }
    public override string ToString()
    {
        return ToString(false);
    }

    public struct MenuElement(byte[] data)
    {
        public string Texture { get; set; } = StaticUtils.GetString(data.Take(0x30).ToArray());

        public bool BgItem { get; set; } = data[0x51] > 0;

        public int PosX { get; set; } = StaticUtils.GetInt32(data, 0x40);
        public int PosY { get; set; } =  StaticUtils.GetInt32(data, 0x44);

        public int Width { get; set; } = StaticUtils.GetInt32(data, 0x48);
        public int Height { get; set; } =  StaticUtils.GetInt32(data, 0x4C);

        public int Dipth { get; set; } = StaticUtils.GetInt32(data, 0x54);
        public int Blend { get; set; } = StaticUtils.GetInt32(data, 0x58);
        public int Index { get; set; } = StaticUtils.GetInt32(data, 0x5C);
    }
}