using System.Text;

namespace FlipnicLib.Formats;

public class FpnMlb : FormatBase
{
    public Dictionary<string, MenuElement[]> Sections { get; set; } = [];
    
    public FpnMlb(byte[] data)
    {
        var sectionCount = GetInt32(data, 0);
        var offset = 0x10;
        var idx = 0;
        while (idx < sectionCount)
        {
            var sectionLabel = GetString(data.Skip(offset).Take(0x20).ToArray());
            var elementCount = GetInt32(data, offset+0x24);
            List<MenuElement> elements = [];
            for (var i = 0; i < elementCount; i++)
            {
                elements.Add(new MenuElement(data.Skip(offset+0x30+(i*0x60)).Take(0x60).ToArray(), sectionLabel));
            }
            Sections.Add(sectionLabel, elements.ToArray());
            offset += 0x30 + elementCount * 0x60;
            idx++;
        }
    }

    public byte[] GetBytes()
    {
        var ms = new MemoryStream();
        for (var i = 0; i < 4; i++)
        {
            ms.Write(BitConverter.GetBytes(Sections.Count));
        }

        foreach (var section in Sections)
        {
            var limTex = ms.Position + 0x20;
            ms.Write(Encoding.ASCII.GetBytes(section.Key));
            ms.Seek(limTex, SeekOrigin.Begin);

            for (var i = 0; i < 4; i++) ms.WriteByte(1);
            for (var i = 0; i < 3; i++) ms.Write(BitConverter.GetBytes(section.Value.Length));

            foreach (var elem in section.Value)
            {
                limTex = ms.Position + 0x40;
                ms.Write(Encoding.ASCII.GetBytes(elem.Texture));
                ms.Seek(limTex, SeekOrigin.Begin);
                ms.Write(BitConverter.GetBytes(elem.PosX));
                ms.Write(BitConverter.GetBytes(elem.PosY));
                ms.Write(BitConverter.GetBytes(elem.Width));
                ms.Write(BitConverter.GetBytes(elem.Height));
                ms.WriteByte(1);
                ms.WriteByte((byte)(elem.BgItem ? 1 : 0));
                ms.WriteByte(1);
                ms.WriteByte(0);
                ms.Write(BitConverter.GetBytes(elem.Dipth));
                ms.Write(BitConverter.GetBytes(elem.Blend));
                ms.Write(BitConverter.GetBytes(elem.Index));
            }
        }
        var data = ms.ToArray();
        ms.Close();
        return data;
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
        public string Texture { get; set; } = GetString(data.Take(0x30).ToArray());

        public bool BgItem { get; set; } = data[0x51] > 0;

        public int PosX { get; set; } = GetInt32(data, 0x40);
        public int PosY { get; set; } =  GetInt32(data, 0x44);

        public int Width { get; set; } = GetInt32(data, 0x48);
        public int Height { get; set; } =  GetInt32(data, 0x4C);

        public int Dipth { get; set; } = GetInt32(data, 0x54);
        public int Blend { get; set; } = GetInt32(data, 0x58);
        public int Index { get; set; } = GetInt32(data, 0x5C);
        
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
        new(){Color = [66, 175, 183], Index = 0, SectionLabel = "OpIconCol"},
        new(){Color = [66, 175, 183], Index = 1, SectionLabel = "OpIconCol"},
        new(){Color = [66, 175, 183], Index = 2, SectionLabel = "OpIconCol"},
        new(){Color = [66, 175, 183], Index = 3, SectionLabel = "OpIconCol"},
        new(){Color = [0xb7, 0x1a, 0x33], Index = 4, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 5, SectionLabel = "OpIconCol"},
        new(){Color = [39, 42, 70], Index = 6, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 7, SectionLabel = "OpIconCol"},
        new(){Color = [39, 42, 70], Index = 8, SectionLabel = "OpIconCol"},
        new(){Color = [55, 78, 110], Index = 9, SectionLabel = "OpIconCol"},
        new(){Color = [55, 78, 110], Index = 10, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 11, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 12, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 13, SectionLabel = "OpIconCol"},
        new(){Color = [117, 170, 243], Index = 14, SectionLabel = "OpIconCol"},
        new(){Color = [60, 149, 194], Index = 0, SectionLabel = "OpVol"},
        new(){Color = [173, 39, 74], Index = 1, SectionLabel = "OpVol"},
        new(){Color = [60, 149, 194], Index = 2, SectionLabel = "OpVol"},
        new(){Color = [173, 39, 74], Index = 3, SectionLabel = "OpVol"},
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
        new(){Color = [178, 255, 29], Index = 0, SectionLabel = "ExpIconCol"},
        new(){Color = [225, 161, 29], Index = 1, SectionLabel = "ExpIconCol"},
        new(){Color = [225, 119, 29], Index = 2, SectionLabel = "ExpIconCol"},
        new(){Color = [225, 94, 29], Index = 3, SectionLabel = "ExpIconCol"},
        new(){Color = [225, 29, 95], Index = 4, SectionLabel = "ExpIconCol"},
        new(){Color = [225, 29, 145], Index = 5, SectionLabel = "ExpIconCol"},
        new(){Color = [205, 26, 208], Index = 6, SectionLabel = "ExpIconCol"},
        new(){Color = [0xb7, 0x1a, 0x32], Index = 7, SectionLabel = "ExpIconCol"},
        new(){Color = [198, 109, 255], Index = 0, SectionLabel = "ExIconcol0"},
        new(){Color = [198, 109, 255], Index = 1, SectionLabel = "ExIconcol0"},
        new(){Color = [198, 109, 255], Index = 2, SectionLabel = "ExIconcol0"},
        new(){Color = [0xe6, 0x16, 0x37], Index = 3, SectionLabel = "ExIconcol0"},
        new(){Color = [125, 0, 120], Index = 4, SectionLabel = "ExIconcol0"},
        new(){Color = [125, 0, 120], Index = 5, SectionLabel = "ExIconcol0"},
        new(){Color = [125, 0, 120], Index = 6, SectionLabel = "ExIconcol0"},
        new(){Color = [133, 103, 221], Index = 0, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [133, 103, 221], Index = 1, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [133, 103, 221], Index = 2, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [133, 103, 221], Index = 3, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [220, 90, 115], Index = 4, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [236, 18, 55], Index = 5, SectionLabel = "ExKeyIconTextCol"},
        new(){Color = [67, 0, 125], Index = 0, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [67, 0, 125], Index = 1, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [67, 0, 125], Index = 2, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [67, 0, 125], Index = 3, SectionLabel = "ExKeyIconTex2Col"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol1"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol1"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol2"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol2"},
        new(){Color = [0xc0, 0x6b, 0xfc], Index = 0, SectionLabel = "ExIconCol3"},
        new(){Color = [0x79, 0x06, 0x74], Index = 1, SectionLabel = "ExIconCol3"},
        new(){Color = [230, 24, 59], Index = 0, SectionLabel = "ExRankIconCol"},
        new(){Color = [230, 24, 59], Index = 1, SectionLabel = "ExRankIconCol"},
        new(){Color = [230, 24, 59], Index = 0, SectionLabel = "ExYnCol"},
        new(){Color = [230, 24, 59], Index = 1, SectionLabel = "ExYnCol"},
        new(){Color = [230, 24, 59], Index = 0, SectionLabel = "ExRankIconCol"},
        new(){Color = [230, 24, 59], Index = 1, SectionLabel = "ExRankIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "GosIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "GosIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "BootIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "BootIconColYN"},
        new(){Color = [0x95, 0xC5, 0x3F], Index = 0, SectionLabel = "MMIconCol"},
        new(){Color = [0xff, 0x91, 0x39], Index = 1, SectionLabel = "MMIconCol"},
        new(){Color = [0xc3, 0x00, 0x57], Index = 2, SectionLabel = "MMIconCol"},
        new(){Color = [0x93, 0x93, 0xe4], Index = 3, SectionLabel = "MMIconCol"},
        new(){Color = [0x12, 0xd9, 0xab], Index = 4, SectionLabel = "MMIconCol"},
        new(){Color = [0xd4, 0xdf, 0x16], Index = 5, SectionLabel = "MMIconCol"},
        new(){Color = [0xc6, 0x6d, 0xd1], Index = 6, SectionLabel = "MMIconCol"},
        new(){Color = [230, 24, 59], Index = 7, SectionLabel = "MMIconCol"},
        new(){Color = [0x95, 0xC5, 0x3F], Index = 0, SectionLabel = "MmSIcon"},
        new(){Color = [0xff, 0x91, 0x39], Index = 1, SectionLabel = "MmSIcon"},
        new(){Color = [0xc3, 0x00, 0x57], Index = 2, SectionLabel = "MmSIcon"},
        new(){Color = [0x93, 0x93, 0xe4], Index = 3, SectionLabel = "MmSIcon"},
        new(){Color = [0x12, 0xd9, 0xab], Index = 4, SectionLabel = "MmSIcon"},
        new(){Color = [0xd4, 0xdf, 0x16], Index = 5, SectionLabel = "MmSIcon"},
        new(){Color = [0xc6, 0x6d, 0xd1], Index = 6, SectionLabel = "MmSIcon"},
        new(){Color = [230, 24, 59], Index = 0, SectionLabel = "SsIconCol"},
        new(){Color = [230, 24, 59], Index = 1, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 2, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 3, SectionLabel = "SsIconCol"},
        new(){Color = [0x80, 0x15, 0x26], Index = 4, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 5, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 6, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 7, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 8, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 9, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 10, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 11, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 12, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 13, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 14, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 15, SectionLabel = "SsIconCol"},
        new(){Color = [230, 24, 59], Index = 16, SectionLabel = "SsIconCol"},
        new(){Color = [151, 0, 56], Index = 17, SectionLabel = "SsIconCol"},
        new(){Color = [151, 0, 56], Index = 18, SectionLabel = "SsIconCol"},
        new(){Color = [151, 0, 56], Index = 19, SectionLabel = "SsIconCol"},
        new(){Color = [78, 78, 78], Index = 0, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 1, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 2, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 3, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 4, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 5, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 6, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 7, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 8, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 9, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 10, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 11, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 12, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 13, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 14, SectionLabel = "SsFIconCol"},
        new(){Color = [78, 78, 78], Index = 15, SectionLabel = "SsFIconCol"},
        new(){Color = [230, 24, 59], Index = 16, SectionLabel = "SsFIconCol"},
        new(){Color = [151, 0, 56], Index = 17, SectionLabel = "SsFIconCol"},
        new(){Color = [151, 0, 56], Index = 18, SectionLabel = "SsFIconCol"},
        new(){Color = [151, 0, 56], Index = 19, SectionLabel = "SsFIconCol"},
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
        new(){Color = [230, 24, 59], Index = 7, SectionLabel = "RuIconCol"},
        new(){Color = [0xbf, 0x06, 0x55], Index = 0, SectionLabel = "SlIconCol"},
        new(){Color = [0xbf, 0x06, 0x55], Index = 1, SectionLabel = "SlIconCol"},
        new(){Color = [230, 24, 59], Index = 2, SectionLabel = "SlIconCol"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 0, SectionLabel = "SlIconColYN"},
        new(){Color = [0xba, 0x1a, 0x32], Index = 1, SectionLabel = "SlIconColYN"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 0, SectionLabel = "SmIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 1, SectionLabel = "SmIconCol"},
        new(){Color = [0x97, 0xd6, 0x25], Index = 2, SectionLabel = "SmIconCol"},
        new(){Color = [230, 24, 59], Index = 3, SectionLabel = "SmIconCol"},
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
        new(){Color = [230, 24, 59], Index = 3, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 4, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 5, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 6, SectionLabel = "SmFIconCol"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 0, SectionLabel = "SmFIconcol2"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 1, SectionLabel = "SmFIconcol2"},
        new(){Color = [0x22, 0x95, 0x1d], Index = 2, SectionLabel = "SmFIconcol2"},
    ];
    
}