namespace FlipnicLib.Formats;

public class Lp4(byte[] data, string fileName)
{
    public enum FileType {
        VariableList,
        StaticModel,
        AnimatedModel,
        Particle,
        HudElement,
        TextAnimation = 0x16
    };
    
    public FileType Type { get; set; } = (FileType)StaticUtils.GetInt32(data, 4);
    public int ModelCount { get; set; } = StaticUtils.GetInt32(data, 0); // not sure if that's what it is anymore
    public bool HasEmbeddedResources { get; set; } = data[0x11] == 0x01;
    public bool Is2dAnimation { get; set; } = data[0x13] == 0x01;
    private List<float[]> verticies = new();
    
    private List<float> rawVerticies = new();

    private string TexturePath { get; set; } = "";
    
    private string FileName { get; set; } = fileName;
    private List<int> ModelOffsets { get; set; } = [];

    public Tim2? Texture { get; set; }

    public List<Model> Models { get; set; } = [];

    public override string ToString()
    {
        var er = HasEmbeddedResources ? "Yes" : "No";
        var i2 = Is2dAnimation ? "Yes" : "No";
        var o = $"""
                Type: {Type.ToString()}
                Model count: {ModelOffsets.Count}
                Has embedded resources: {er}
                Is 2D animation: {i2}
                
                """;
        if (Type != FileType.StaticModel) return o;
        o += $"""

              Models:
              
              """;
        string[] cols = ["Name", "Address", "Scale", "Offset", "Texture", "Polygons"];
        List<string[]> rows = [];
        rows.AddRange(Models.Select(model => model.GetRow()));
        o += StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput);
        return o;
    }

    // this parser seems to fail most of the time, so further adjustments maybe needed
    private void GetModelOffset()
    {
        try
        {
            var i = 0xA0;
            i += StaticUtils.GetInt32(data, 8) * 0x10;
            while ((i < data.Length) && (i >= 0))
            {
                var sectionCount = StaticUtils.GetInt32(data, i + 4) - 1;
                var modelOffset = i + 0x20;
                var model = new Model { Address = modelOffset, Name = StaticUtils.GetStringAt(data, modelOffset) };
                i += 0x30; // name and params
                model.Scale =
                [
                    StaticUtils.GetFloat(data, i + 0x40), StaticUtils.GetFloat(data, i + 0x54),
                    StaticUtils.GetFloat(data, i + 0x68)
                ];
                model.Offset =
                [
                    StaticUtils.GetFloat(data, i + 0x70), StaticUtils.GetFloat(data, i + 0x74),
                    StaticUtils.GetFloat(data, i + 0x78)
                ];
                i += 0x80; // position, size, etc
                i += 0xA0 * sectionCount; // some unknown sections
                i += 0xA0; // more parameters before start of model

                // model
                i += 0x20; // model identifier, I guess?
                var vectCount = StaticUtils.GetInt32(data, i);
                var normalCount = StaticUtils.GetInt32(data, i + 4);
                var textureCoordCount = StaticUtils.GetInt32(data, i + 12);
                model.AppendVerticies(i, data);
                i += 0x10; // the counts
                i += 0x10 * vectCount; // vertices
                i += 0x8 * normalCount; // normals
                i += 0x8 * textureCoordCount; // texture coordinates
                i += 0x80; // weird section that says "prefix" something-something
                model.Texture = StaticUtils.GetStringAt(data, i);
                i += 0x30; // footer containing the name of the texture
                Models.Add(model);
            }

            if (Models[0].RawVertices.Count != 0) return;
            Models.Clear();
            OldMethod();
        }
        catch
        {
            OldMethod();
        }
    }
    
    private void OldMethod()
    {
        Console.WriteLine("Warning: failed to parse LP4 file correctly, falling back to brute-force method!");
        var i = 0;
        while (i < data.Length - 0x20)
        {
            int f, f2, f3, f4;
            f = BitConverter.ToInt32([.. data.Skip(i).Take(4)], 0);
            f2 = BitConverter.ToInt32([.. data.Skip(i + 0x10).Take(4)], 0);
            f3 = BitConverter.ToInt32([.. data.Skip(i + 0x14).Take(4)], 0);
            f4 = BitConverter.ToInt32([.. data.Skip(i + 0x1c).Take(4)], 0);
            if ((f > 0) && (f2 == f3) && (f3 == f4))
            {
                var len = f2;
                if ((len > 0) && (len < data.Length))
                {
                    AppendVerticies(i+0x10, len);
                    i += 2*len * 0x10 + 0xA0;
                    break;
                }
            }
            i += 0x10;
        }
        TexturePath = StaticUtils.GetString(data.Skip(i).Take(0x20).ToArray());

        if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                TexturePath.ToUpper()))) return;
        var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
            TexturePath.ToUpper()));
        var d = new byte[fs.Length];
        fs.ReadExactly(d, 0, d.Length);
        Texture = new Tim2(d, TexturePath);
    }

    private void AppendVerticies(int offset, int forced_length = -1)
    {
        var len = BitConverter.ToInt32(data, offset);
        var texOffset = offset + (len * 0x18) + 0x10;
        var div = 4096f;
        var uvOffset = texOffset;
        for (var j = offset + 0x10; j < offset + len * 0x10 - 0x10; j += 0x10)
        {
            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset).Take(8).ToArray())[0]);
            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset).Take(8).ToArray())[1]);
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 4).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 8).Take(4).ToArray(), 0));

            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray())[0]);
            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray())[1]);
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x10).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x14).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x18).Take(4).ToArray(), 0));

            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray())[0]);
            rawVerticies.Add(Model.DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray())[1]);
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x20).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x24).Take(4).ToArray(), 0));
            rawVerticies.Add(BitConverter.ToSingle(data.Skip(j + 0x28).Take(4).ToArray(), 0));
            uvOffset += 8;
        }
    }
    
    public void Read()
    {
        GetModelOffset();
        if (rawVerticies.Count > 0) return;
        if (Models.Count == 0)
        {
            return;
        }
        if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                                        Models[0].Texture.ToUpper()))) return;
        var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
            Models[0].Texture.ToUpper()));
        var d = new byte[fs.Length];
        fs.ReadExactly(d, 0, d.Length);
        Texture = new Tim2(d, Models[0].Texture);
    }

    public float[] GetVerticies()
    {
        if ((rawVerticies.Count == 0) && (Models.Count > 0))
        {
            return Models[0].RawVertices.ToArray();
        }
        return rawVerticies.ToArray();
    }
}

public class Model
{
    public string Name { get; set; }
    public string Texture { get; set; }
    
    public float[] Scale { get; set; }
    public float[] Offset { get; set; }
    public int Address { get; set; }

    public List<float> RawVertices { get; set; } = [];

    public string[] GetRow()
    {
        return
        [
            Name, Address.ToString("X"),
            $"{StaticUtils.DotFloatString(Scale[0])}x{StaticUtils.DotFloatString(Scale[1])}x{StaticUtils.DotFloatString(Scale[2])}",
            $"{StaticUtils.DotFloatString(Offset[0])}x{StaticUtils.DotFloatString(Offset[1])}x{StaticUtils.DotFloatString(Offset[2])}",
            Texture,
            RawVertices.Count.ToString()
        ];
    }
    
    

    public void AppendVerticies(int offset, byte[] data)
    {
        var len = BitConverter.ToInt32(data, offset);
        var texOffset = offset + (len * 0x18) + 0x10;
        var div = 4096f;
        var uvOffset = texOffset;
        for (var j = offset + 0x10; j < offset + len * 0x10 - 0x10; j += 0x10)
        {
            RawVertices.Add(DecodeCoords(data.Skip(uvOffset).Take(8).ToArray())[0]);
            RawVertices.Add(DecodeCoords(data.Skip(uvOffset).Take(8).ToArray())[1]);
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 4).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 8).Take(4).ToArray(), 0));

            RawVertices.Add(DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray())[0]);
            RawVertices.Add(DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray())[1]);
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x10).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x14).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x18).Take(4).ToArray(), 0));

            RawVertices.Add(DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray())[0]);
            RawVertices.Add(DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray())[1]);
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x20).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x24).Take(4).ToArray(), 0));
            RawVertices.Add(BitConverter.ToSingle(data.Skip(j + 0x28).Take(4).ToArray(), 0));
            uvOffset += 8;
        }
    }

    public static float[] DecodeCoords(byte[] data)
    {
        var div = BitConverter.ToInt16(data.Skip(4).Take(2).ToArray(), 0);
        var unk = BitConverter.ToInt16(data.Skip(6).Take(2).ToArray(), 0); // no idea, reflection mapping maybe?
        var fx = BitConverter.ToInt16(data.Take(2).ToArray(), 0);
        var fy = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray(), 0);
        return [(float)fx/div, -(float)fy/div]; // invert, because otherwise it's upside-down
    }
}