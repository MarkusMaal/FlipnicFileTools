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
                elements.Add(new MenuElement(data.Skip(offset+0x30+(i*0x60)).Take(0x60).ToArray(), SectionLabel));
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
    
    public struct MenuElement(byte[] data, string sectionLabel)
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
        
        public override string ToString()
        {
            return $"{sectionLabel} | {Texture} ({Index})";
        }
    }

    public struct MenuColor
    {
        public string SectionLabel { get; set; }
        public int Index { get; set; }
        public byte[] Color { get; set; }
    }
    
    
    // for replacing Col textures with the colors the game actually uses (the colors are hard-coded for now)
    public readonly MenuColor[] MenuColors =
    [
        new(){Color = [0x41, 0xa9, 0xb0], Index = 0, SectionLabel = "OpIconCol"},
        new(){Color = [0x41, 0xa9, 0xb0], Index = 1, SectionLabel = "OpIconCol"},
        new(){Color = [0x41, 0xa9, 0xb0], Index = 2, SectionLabel = "OpIconCol"},
        new(){Color = [0x41, 0xa9, 0xb0], Index = 3, SectionLabel = "OpIconCol"},
        new(){Color = [0xb7, 0x1a, 0x33], Index = 4, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 5, SectionLabel = "OpIconCol"},
        new(){Color = [0x44, 0x4e, 0x4e], Index = 6, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 7, SectionLabel = "OpIconCol"},
        new(){Color = [0x44, 0x4e, 0x4e], Index = 8, SectionLabel = "OpIconCol"},
        new(){Color = [0x38, 0x4b, 0x69], Index = 9, SectionLabel = "OpIconCol"},
        new(){Color = [0x38, 0x4b, 0x69], Index = 10, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 11, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 12, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 13, SectionLabel = "OpIconCol"},
        new(){Color = [0x70, 0xa2, 0xeb], Index = 14, SectionLabel = "OpIconCol"},
        new(){Color = [0x3b, 0x8f, 0xba], Index = 0, SectionLabel = "OpVol"},
        new(){Color = [0xa7, 0x2a, 0x49], Index = 1, SectionLabel = "OpVol"},
        new(){Color = [0x3b, 0x8f, 0xba], Index = 2, SectionLabel = "OpVol"},
        new(){Color = [0xa7, 0x2a, 0x49], Index = 3, SectionLabel = "OpVol"},
        new(){Color = [0xb7, 0x1a, 0x33], Index = 12, SectionLabel = "StFIconCol"},
        new(){Color = [0x31, 0xc8, 0xb5], Index = 11, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 9, SectionLabel = "StFIconCol"},
        new(){Color = [0x31, 0xc8, 0xb5], Index = 8, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 7, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 6, SectionLabel = "StFIconCol"},
        new(){Color = [0x31, 0xc8, 0xb5], Index = 5, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 4, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 3, SectionLabel = "StFIconCol"},
        new(){Color = [0x31, 0xc8, 0xb5], Index = 2, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 1, SectionLabel = "StFIconCol"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 0, SectionLabel = "StFIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 0, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 1, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 2, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 3, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 10, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 11, SectionLabel = "StFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 12, SectionLabel = "StFCeltext"},
        new(){Color = [0xb7, 0x1a, 0x33], Index = 12, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 11, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 10, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 9, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 8, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 7, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 6, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 5, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 4, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 3, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 2, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 1, SectionLabel = "StColSet"},
        new(){Color = [0x98, 0xd6, 0x25], Index = 0, SectionLabel = "StColSet"},
        new(){Color = [0x00, 0x00, 0x00], Index = 0, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 1, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 2, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 3, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 10, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 11, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 12, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 13, SectionLabel = "StCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 33, SectionLabel = "StCeltext"},
        new(){Color = [0xac, 0xdd, 0x21], Index = 0, SectionLabel = "ExpIconCol"},
        new(){Color = [0xd9, 0x9a, 0x21], Index = 1, SectionLabel = "ExpIconCol"},
        new(){Color = [0xd9, 0x72, 0x21], Index = 2, SectionLabel = "ExpIconCol"},
        new(){Color = [0xda, 0x5b, 0x22], Index = 3, SectionLabel = "ExpIconCol"},
        new(){Color = [0xd9, 0x21, 0x5b], Index = 4, SectionLabel = "ExpIconCol"},
        new(){Color = [0xd9, 0x21, 0x8b], Index = 5, SectionLabel = "ExpIconCol"},
        new(){Color = [0xc5, 0x1e, 0xc8], Index = 6, SectionLabel = "ExpIconCol"},
        new(){Color = [0xb7, 0x1a, 0x32], Index = 7, SectionLabel = "ExpIconCol"},
        new(){Color = [0xbf, 0x6a, 0xfb], Index = 0, SectionLabel = "ExIconcol0"},
        new(){Color = [0xbf, 0x6a, 0xfb], Index = 1, SectionLabel = "ExIconcol0"},
        new(){Color = [0xbf, 0x6a, 0xfb], Index = 2, SectionLabel = "ExIconcol0"},
        new(){Color = [0xe6, 0x16, 0x37], Index = 3, SectionLabel = "ExIconcol0"},
        new(){Color = [0x79, 0x07, 0x73], Index = 4, SectionLabel = "ExIconcol0"},
        new(){Color = [0x79, 0x07, 0x73], Index = 5, SectionLabel = "ExIconcol0"},
        new(){Color = [0x79, 0x07, 0x73], Index = 6, SectionLabel = "ExIconcol0"},
        new(){Color = [0x7f, 0x63, 0xd5], Index = 0, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0x7f, 0x63, 0xd5], Index = 1, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0x7f, 0x63, 0xd5], Index = 2, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0x7f, 0x63, 0xd5], Index = 3, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0xd6, 0x57, 0x6f], Index = 4, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0xe6, 0x16, 0x38], Index = 5, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [0x42, 0x00, 0x79], Index = 0, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [0x42, 0x00, 0x79], Index = 1, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [0x42, 0x00, 0x79], Index = 2, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [0x42, 0x00, 0x79], Index = 3, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol1"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol1"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol2"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol2"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol3"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol3"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 0, SectionLabel = "ExRankIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 1, SectionLabel = "ExRankIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 0, SectionLabel = "ExYnCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 1, SectionLabel = "ExYnCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 0, SectionLabel = "ExRankIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 1, SectionLabel = "ExRankIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "GosIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "GosIconColYN"},
        new(){Color = [0x91, 0xc0, 0x3f], Index = 0, SectionLabel = "MMIconCol"},
        new(){Color = [0xfb, 0x8c, 0x39], Index = 1, SectionLabel = "MMIconCol"},
        new(){Color = [0xbd, 0x06, 0x55], Index = 2, SectionLabel = "MMIconCol"},
        new(){Color = [0x8c, 0x8c, 0xdb], Index = 3, SectionLabel = "MMIconCol"},
        new(){Color = [0x16, 0xd4, 0xa5], Index = 4, SectionLabel = "MMIconCol"},
        new(){Color = [0xce, 0xda, 0x1a], Index = 5, SectionLabel = "MMIconCol"},
        new(){Color = [0xbf, 0x6a, 0xcc], Index = 6, SectionLabel = "MMIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 7, SectionLabel = "MMIconCol"},
        new(){Color = [0x91, 0xc0, 0x3f], Index = 0, SectionLabel = "MmSIcon"},
        new(){Color = [0xfb, 0x8c, 0x39], Index = 1, SectionLabel = "MmSIcon"},
        new(){Color = [0xbd, 0x06, 0x55], Index = 2, SectionLabel = "MmSIcon"},
        new(){Color = [0x8c, 0x8c, 0xdb], Index = 3, SectionLabel = "MmSIcon"},
        new(){Color = [0x16, 0xd4, 0xa5], Index = 4, SectionLabel = "MmSIcon"},
        new(){Color = [0xce, 0xda, 0x1a], Index = 5, SectionLabel = "MmSIcon"},
        new(){Color = [0xbf, 0x6a, 0xcc], Index = 6, SectionLabel = "MmSIcon"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 0, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 1, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 2, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 3, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 4, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 5, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 6, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 7, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 8, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 9, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 10, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 11, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 12, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 13, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 14, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 15, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 16, SectionLabel = "SsIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 17, SectionLabel = "SsIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 18, SectionLabel = "SsIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 19, SectionLabel = "SsIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 0, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 1, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 2, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 3, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 4, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 5, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 6, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 7, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 8, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 9, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 10, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 11, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 12, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 13, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 14, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 15, SectionLabel = "SsFIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 16, SectionLabel = "SsFIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 17, SectionLabel = "SsFIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 18, SectionLabel = "SsFIconCol"},
        new(){Color = [0x81, 0x14, 0x25], Index = 19, SectionLabel = "SsFIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "PmIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "PmIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 2, SectionLabel = "PmIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 3, SectionLabel = "PmIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "PmenuIcon2col"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "PmenuIcon2col"},
        new(){Color = [0x00, 0x00, 0x00], Index = 0, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 1, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 2, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 3, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 4, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 5, SectionLabel = "RuIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 6, SectionLabel = "RuIconCol"},
        new(){Color = [0xb9, 0x19, 0x32], Index = 7, SectionLabel = "RuIconCol"},
        new(){Color = [0xbf, 0x06, 0x55], Index = 0, SectionLabel = "SlIconCol"},
        new(){Color = [0xbf, 0x06, 0x55], Index = 1, SectionLabel = "SlIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 2, SectionLabel = "SlIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "SlIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "SlIconColYN"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 0, SectionLabel = "SmIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 1, SectionLabel = "SmIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 2, SectionLabel = "SmIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 3, SectionLabel = "SmIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 4, SectionLabel = "SmIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 5, SectionLabel = "SmIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 6, SectionLabel = "SmIconCol"},
        new(){Color = [0x00, 0x00, 0x00], Index = 0, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 1, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 2, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 3, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 4, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 5, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 6, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 7, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 8, SectionLabel = "SmCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 0, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 1, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 2, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 3, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 10, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 11, SectionLabel = "SmFCeltext"},
        new(){Color = [0x00, 0x00, 0x00], Index = 12, SectionLabel = "SmFCeltext"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 0, SectionLabel = "SmFIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 1, SectionLabel = "SmFIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 2, SectionLabel = "SmFIconCol"},
        new(){Color = [0xb9, 0x1a, 0x32], Index = 3, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 4, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 5, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 6, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 0, SectionLabel = "SmFIconcol2"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 1, SectionLabel = "SmFIconcol2"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 2, SectionLabel = "SmFIconcol2"},
    ];
    
}