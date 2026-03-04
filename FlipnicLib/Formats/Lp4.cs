using System.Diagnostics;
using System.Text;

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
    private List<float[]> verticies = [];

    private readonly List<float[]> _boundingBox = [];
    

    private int _animationJoints
    {
        get
        {
            if (data[0x11] != 0x01) return 0;
            var additionalDataLength = StaticUtils.GetInt32(data, 8);
            return StaticUtils.GetInt32(data.Skip(0xF4 + additionalDataLength * 0x10).Take(4).ToArray(), 0);
        }
    }

    private List<float> rawVerticies = [];

    private string TexturePath { get; set; } = "";
    
    private string FileName { get; set; } = fileName;
    private List<int> ModelOffsets { get; set; } = [];

    public byte[] Texture { get; set; }

    public List<Model> Models { get; set; } = [];
    
    public Model? SelectedModel { get; set; }

    public override string ToString()
    {
        var er = HasEmbeddedResources ? "Yes" : "No";
        var i2 = Is2dAnimation ? "Yes" : "No";
        var o = $"""
                Type: {Type.ToString()}
                Model count: {ModelOffsets.Count}
                Has bounding box: {er}
                Is 2D animation: {i2}
                Timelines: {StaticUtils.GetInt32(data.Skip(8).Take(4).ToArray(), 0)}
                Animation joints: {_animationJoints}
                
                """;
        string[] cols = ["X", "Y", "Z"];
        List<string[]> rows = [];
        rows.AddRange(_boundingBox.Select(vertex => (string[])[StaticUtils.DotFloatString(vertex[0]), StaticUtils.DotFloatString(vertex[1]), StaticUtils.DotFloatString(vertex[2])]));
        o += $"""
              
              Bounding box:
              {StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput)}
              
              """;
        rows.Clear();
        //if (Type != FileType.StaticModel) return o;
        o += $"""

              Models:
              
              """;
        cols = ["Name", "Address", "Scale", "Offset", "Texture", "Polygons"];
        rows.AddRange(Models.Select(model => model.GetRow()));
        o += StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput);
        if (_animationJoints <= 0) return o;
        o += $"""

              Joints:

              """;
        cols = ["Name", "Vertices", "Position", "Size"];
        rows.Clear();
        if (Models.Count <= 0) return o;
        rows.AddRange(Models[0].AnimationJoints.Values.Select(jnt => new[] { jnt.Name, (jnt.Indicies ?? []).Length.ToString(), $"{StaticUtils.DotFloatString(jnt.Position?[0] ?? float.NaN)}x{StaticUtils.DotFloatString(jnt.Position?[1] ?? float.NaN)}x{StaticUtils.DotFloatString(jnt.Position?[2] ?? float.NaN)}", $"{StaticUtils.DotFloatString(jnt.Skew?[0] ?? float.NaN)}x{StaticUtils.DotFloatString(jnt.Skew?[5] ?? float.NaN)}x{StaticUtils.DotFloatString(jnt.Skew?[8] ?? float.NaN)}" }));
        o += StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput);
        return o;
    }

    /// <summary>
    /// Swap the selected model
    /// </summary>
    /// <param name="model">Model object from the Models list</param>
    public void SetSelectedModel(Model model)
    {
        SelectedModel = model;
        if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                SelectedModel.Texture.ToUpper()))) return;
        var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
            SelectedModel.Texture.ToUpper()));
        var d = new byte[fs.Length];
        fs.ReadExactly(d, 0, d.Length);
        if (SelectedModel.TextureCache == null)
        {
            var tim2 = new Tim2(d, SelectedModel.Texture);
            var ms = new MemoryStream();
            tim2.SavePng(ms);
            Texture = ms.ToArray();
            SelectedModel.TextureCache = ms.ToArray();
        }
        else
        {
            Texture = SelectedModel.TextureCache;
        }
    }

    // this parser seems to fail most of the time, so further adjustments maybe needed
    private void GetModelOffset()
    {
        try
        {
            var hasBoundingBox = data[17] == 0x01;
            var additionalDataLength = StaticUtils.GetInt32(data, 8); 
            var i = 0x20;
            if (hasBoundingBox) i += 0x80;
            i += additionalDataLength * 0x10;
            while ((i < data.Length) && (i >= 0))
            {
                var attributeSetCount = StaticUtils.GetInt32(data, i + 4);
                var extraUnknownDataCount = StaticUtils.GetInt32(data, i + 12);
                var modelOffset = i + 0x20;
                var model = new Model { Address = modelOffset, Name = StaticUtils.GetStringAt(data, modelOffset) };
                var layoutCounts = StaticUtils.GetInt32(data, i + 4);
                var hasHitbox = data[i + 0xC] == 0x01;
                i += 0x30; // name and params
                model.Lightmap = [];
                i += 0x10;
                for (var k = 0; k < layoutCounts; k++)
                {
                    StaticUtils.LiveLoadStatus = $"Parsing layouts ({StaticUtils.DotFloatString((float)Math.Round(k/(double)layoutCounts*100.0, 2))}% complete)";
                    var keyframeCount = StaticUtils.GetInt32(data, i);
                    var animationJoints = StaticUtils.GetInt32(data, i + 0x14);
                    var hasLightMapData = data[i + 0x20] == 0x01;
                    var hasUnknownData = data[i + 0x24] == 0x01;
                    var lightMapLength = StaticUtils.GetInt32(data, i + 0x28);
                    
                    if (animationJoints > 65536)
                    {
                        StaticUtils.DecodeColors("~-CError~--: Animation joint count was too large, continuing would cause hangs! Parser was halted!\n");
                        BruteForceMethod();
                        return;
                    }
                    if (keyframeCount > 65536)
                    {
                        StaticUtils.DecodeColors("~-CError~--: Keyframe count was too large, continuing would cause hangs! Parser was halted!\n");
                        BruteForceMethod();
                        return;
                    }
                    if (lightMapLength > 65536)
                    {
                        StaticUtils.DecodeColors("~-CError~--: Lightmap length was too large, continuing would cause hangs! Parser was halted!\n");
                        BruteForceMethod();
                        return;
                    }
                    model.Scale =
                    [
                        StaticUtils.GetFloat(data, i + 0x30), StaticUtils.GetFloat(data, i + 0x44),
                        StaticUtils.GetFloat(data, i + 0x58)
                    ];
                    model.Offset =
                    [
                        StaticUtils.GetFloat(data, i + 0x60), StaticUtils.GetFloat(data, i + 0x64),
                        StaticUtils.GetFloat(data, i + 0x68)
                    ];
                    i += 0x80;
                    if (hasUnknownData)
                    {
                        i += 0x30 * (StaticUtils.GetInt32(data, 0x24));
                    }
                    for (var j = 0; j < (hasLightMapData ? lightMapLength : 0); j++)
                    {
                        StaticUtils.LiveLoadStatus = $"Parsing lightmaps";
                        model.Lightmap.Add([
                            StaticUtils.GetFloat(data, i), StaticUtils.GetFloat(data, i + 0x4),
                            StaticUtils.GetFloat(data, i + 0x8), StaticUtils.GetFloat(data, i + 0xC)
                        ]);
                        i += 0x10;
                    }

                    i += keyframeCount * 0x30;
                    for (var a = 0; a < animationJoints; a++)
                    {
                        StaticUtils.LiveLoadStatus = $"Parsing animation joints ({StaticUtils.DotFloatString((float)Math.Round(a/(double)animationJoints*100.0, 2))}% complete)";
                        var sp = i + (0x60 * a);
                        var name = StaticUtils.GetString(data.Skip(sp - 0x10).ToArray());
                        if (!Ascii.IsValid(name) || (name == "")) continue;
                        var skew = new List<float?>();
                        var pos = new List<float?>();
                        for (var b = sp + 0x20; b < sp + 0x50; b+=4)
                        {
                            skew.Add(StaticUtils.GetFloat(data, b));
                        }
                        for (var b = sp + 0x50; b < sp + 0x5C; b+=4)
                        {
                            pos.Add(StaticUtils.GetFloat(data, b));
                        }

                        while (model.AnimationJoints.ContainsKey(name))
                        {
                            name += " (1)";
                        }
                        model.AnimationJoints.Add(name, new Joint()
                        {
                            Name = name,
                            Skew = skew.ToArray(),
                            Position = pos.ToArray()
                        });
                    }
                    i += 0x60 * animationJoints;
                }

                if (hasHitbox)
                {
                    i += 0x80;
                }

                // model
                var animIndices = StaticUtils.GetInt32(data, i + 0x1C);
                if (animIndices > 65536)
                {
                    StaticUtils.DecodeColors("~-CError~--: Animation indices count was too large, continuing would cause hangs! Parser was halted!\n");
                    BruteForceMethod();
                    return;
                }
                var padding = 0x10 * (StaticUtils.GetInt32(data, i + 0x18));
                i += 0x20; // model identifier, I guess?
                i += padding;
                if (animIndices > 0) i += 0x10;
                for (var h = 0; h < animIndices; h++)
                {
                    StaticUtils.LiveLoadStatus = $"Parsing animation indicies ({StaticUtils.DotFloatString((float)Math.Round(h/(double)animIndices*100.0, 2))}% complete)";
                    var count = StaticUtils.GetInt32(data, i+0x20);
                    var name = StaticUtils.GetString(data.Skip(i).Take(20).ToArray());
                    if (model.AnimationJoints.ContainsKey(name))
                    {
                        model.AnimationJoints[name].DecodeIndicies(data.Skip(i).Take(0x30 + count * 0x10).ToArray());
                    }
                    else
                    {
                        var j = new Joint()
                        {
                            Name = name
                        };
                        j.DecodeIndicies(data.Skip(i).Take(0x30 + count * 0x10).ToArray());
                        model.AnimationJoints.Add(name, j);
                    }

                    i += 0x30 + count * 0x10;
                }
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
                if (model.AnimationJoints.Count > 0) break;
            }


            if (Models.Count > 0)
            {
                SelectedModel = Models[0];
            }

            if (SelectedModel?.RawVertices.Count != 0)
            {
                StaticUtils.DecodeColors("~-ASuccess~--: Successfully decoded the LP4 file!");
                Console.WriteLine();
                return;
            }
            Models.Clear();
            BruteForceMethod();
        }
        catch when (!Debugger.IsAttached)
        {
            BruteForceMethod();
        }
    }
    
    private void BruteForceMethod()
    {
        StaticUtils.DecodeColors("~-EWarning~--: failed to parse LP4 file correctly, falling back to brute-force method!\n");
        int i;
        var modelIdx = 0;
        List<string> names = [];
        for (i = 0x30; i < data.Length - 0x20; i += 0x10)
        {
            if (StaticUtils.GetInt32(data, i + 0x2C) == 0 && StaticUtils.GetInt32(data, i + 0x3C) == 0) continue; // joint definition, ignore those
            var nameTest =  StaticUtils.GetStringAt(data, i);
            if (nameTest.Contains("JNT")) continue;
            if (names.Contains(nameTest)) continue;
            var validLetters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_";
            var isValid = nameTest.All(letter => validLetters.Contains(letter));
            if (!isValid || !Ascii.IsValid(nameTest) || nameTest.Length < 0x3) continue;
            names.Add(nameTest);
        }

        names = names[1..];
        for (i = 0; i < data.Length - 0x20; i+=0x10)
        {
            StaticUtils.LiveLoadStatus = $"Scanning for 3D model data ({StaticUtils.DotFloatString((float)Math.Round(i/(double)data.Length*100.0, 2))}% complete)";
            var c1 = StaticUtils.GetInt32(data, i);
            var c2 = StaticUtils.GetInt32(data, i+4);
            var c3 = StaticUtils.GetInt32(data, i+8);
            var c4 = StaticUtils.GetInt32(data, i+12);
            if (c1 > 0)
            {
                if (i + 0x10 + c1 * 0x10 > data.Length) continue;
                var ok = true;
                for (var j = i + 0x10; j < i + 0x10 + c1 * 0x10; j += 0x10)
                {
                    if (float.IsNaN(StaticUtils.GetFloat(data, j))) ok = false;
                    if (float.IsNaN(StaticUtils.GetFloat(data, j+4))) ok = false;
                    if (float.IsNaN(StaticUtils.GetFloat(data, j+8))) ok = false;
                    if (Math.Abs(StaticUtils.GetFloat(data, j+12) - 1f) > 0.0001) ok = false;
                }

                if (!ok) continue;
            }

            if (c2 > 0)
            {
                if (i + 0x10 + c1 * 0x10 + c2 * 0x8 > data.Length) continue;
                var ok = true;
                for (var j = i + 0x10 + c1 * 0x10; j < i + 0x10 * c1 + 0x10 + c2 * 0x8; j += 0x8)
                {
                    if (StaticUtils.GetInt16(data, j + 6) == 0) continue;
                    ok = false;
                    break;
                }

                if (!ok) continue;
            }

            if (c4 > 0)
            {
                if (i + 0x10 + c1 * 0x10 + c2 * 0x8 + c3 * 0x4 + c4 * 0x8 > data.Length) continue;
                var ok = true;
                var start = i + 0x10 + c1 * 0x10 + c2 * 0x8 + c3 * 0x4;
                var end =  i + 0x10 + c1 * 0x10 + c2 * 0x8 + c3 * 0x4 + c4 * 0x8;
                for (var j = start; j < end; j += 0x8)
                {
                    ok = StaticUtils.GetUInt16(data, j + 6) == 0x00 ||
                         StaticUtils.GetUInt16(data, j + 6) == 0x01 ||
                         StaticUtils.GetUInt16(data, j + 6) == 0x8000 ||
                         StaticUtils.GetUInt16(data, j + 6) == 0x8001;
                    if (!ok) break;
                }

                if (!ok) continue;
            }

            if (c1 == 0) continue;
            if (i < 0x80) continue;
            try
            {
                var modelEnd = i + 0x10 + (c1 * 0x10) + (c2 * 0x8) + (c3 * 0x4) + (c4 * 0x08);
                var tm = new Model
                {
                    Name = $"Unrecognized model {modelIdx++}"
                };
                if (names.Count > Models.Count)
                {
                    tm.Name = names[Models.Count];
                }
                tm.AppendVerticies(i, data);
                tm.Offset = [0, 0 ,0];
                tm.Address = i;
                tm.Scale = [1, 1, 1];

                if (tm.RawVertices.Count > 0)
                {
                    StaticUtils.DecodeColors($"~-ASuccess~--: Detected valid model data at offset 0x{i:X}\n");

                    for (var j = 0x0; j < 0x500; j++)
                    {
                        var texOffset = modelEnd + (0x10 * j);
                        if (texOffset >= data.Length) break;
                        tm.Texture = StaticUtils.GetString(data.Skip(texOffset).Take(0x20).ToArray());
                        if (!Ascii.IsValid(tm.Texture) || tm.Texture.Length == 0 ||
                            !tm.Texture.ToLower().EndsWith(".tm2")) continue;
                        break;
                    }
                    if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                            tm.Texture.ToUpper())))
                    {
                        StaticUtils.DecodeColors("~-EWarning~--: The model does not have a texture\n");
                    }

                    Models.Add(tm);
                }
                else
                {
                    i += 0x10;
                }
            }
            catch
            {
                StaticUtils.DecodeColors($"~-CError~--: Attempt to read from offset 0x{i:X} threw an error\n");
            }
        }

        if (Models.Count <= 0) return;
        foreach (var model in Models.ToArray().Reverse().ToArray())
        {
            StaticUtils.LiveLoadStatus = "Precaching textures";
            SetSelectedModel(model);
        }

    }
    
    /// <summary>
    /// Process the data provided
    /// </summary>
    public void Read()
    {
        try
        {
            GetModelOffset();
            ParseBoundingBox();
            if (rawVerticies.Count > 0) return;
            if (SelectedModel == null) return;
            if (Models.Count == 0)
            {
                return;
            }
            if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                    SelectedModel.Texture.ToUpper()))) return;
            var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                SelectedModel.Texture.ToUpper()));
            var d = new byte[fs.Length];
            fs.ReadExactly(d, 0, d.Length);
            var tim2 = new Tim2(d, SelectedModel.Texture);
            var ms = new MemoryStream();
            tim2.SavePng(ms);
            Texture = ms.ToArray();
            foreach (var model in Models.ToArray().Reverse().ToArray())
            {
                StaticUtils.LiveLoadStatus = "Precaching textures";
                SetSelectedModel(model);
            }
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            StaticUtils.DecodeColors($"~-CError~--: LP4.Read method exception — {ex.Message}\n");
            return;
        }
    }

    /// <summary>
    /// Get the 3D float array of the selected model
    /// </summary>
    /// <returns>An array containing chunks of 8 * sizeof(float), where first 2 entries are XY UV coordinates, next 3 are XYZ vertex coordinates, final 3 are XYZ normal coordinates</returns>
    public float[] GetVerticies()
    {
        if ((rawVerticies.Count == 0) && (Models.Count > 0) && (SelectedModel != null))
        {
            return SelectedModel.RawVertices.ToArray();
        }
        return rawVerticies.ToArray();
    }

    private void ParseBoundingBox()
    {
        if (data[0x11] != 0x01) return;
        var additionalDataLength = StaticUtils.GetInt32(data, 8);
        var boxRaw = data.Skip(0x20 + additionalDataLength * 0x10).Take(0x80).ToArray();
        var boxRawFloats = new List<float[]>();
        for (var i = 0; i < boxRaw.Length; i += 0x10)
        {
            boxRawFloats.Add([StaticUtils.GetFloat(boxRaw, i), StaticUtils.GetFloat(boxRaw, i+4), StaticUtils.GetFloat(boxRaw, i+8)]);
        }
        // basically the points in the file define the top and bottom side of the rectangle let's call these 0 1 2 3 4 5 6 7,
        // where 0 1 2 3 are the points of the first rectangle in a 3D space and 4 5 6 7 define the second rectangle
        //
        // with some very basic 3D geometry we can simply "connect the dots" to get the remaining triangles required to generate a full
        // box shape
        foreach (var i in new[]{ 0, 1, 2, 1, 2, 3, 4, 5, 6, 6, 7, 5, 2, 3, 6, 3, 7, 6, 0, 1, 5, 0, 5, 4, 2, 0, 4, 6, 4, 2, 1, 3, 7, 1, 5, 7 })
        {
            _boundingBox.Add(boxRawFloats[i]);   
        }
    }

    public float[] GetBoundingBox()
    {
        var floats = new List<float>();
        foreach (var vtx in _boundingBox)
        {
            floats.Add(0f);
            floats.Add(0f);
            floats.AddRange(vtx);
            floats.Add(0f);
            floats.Add(0f);
            floats.Add(0f);
        }

        return floats.ToArray();
    }
}

public class Joint
{
    public string Name { get; set; }
    public int[] Indicies { get; set; }
    public float[] UnknownFloats { get; set; }
    
    public float?[] Skew { get; set; }
    public float?[] Position { get; set; }
    
    public Joint() {}

    public void DecodeIndicies(byte[] data)
    {
        Name = StaticUtils.GetString(data.Take(0x20).ToArray());
        var count =  StaticUtils.GetInt32(data, 0x20);
        List<int> indicies = [];
        List<float> unknownFloats = [];
        for (var i = 0x30; i < count * 0x10 + 0x30; i += 0x10)
        {
            indicies.Add(StaticUtils.GetInt32(data, i));
            unknownFloats.Add(StaticUtils.GetFloat(data, i + 4));
        }

        Indicies = indicies.ToArray();
        UnknownFloats = unknownFloats.ToArray();
    }
}

public class Model
{
    public string Name { get; set; }
    public string Texture { get; set; }
    
    public float[] Scale { get; set; }
    public float[] Offset { get; set; }
    public int Address { get; set; }
    public byte[]? TextureCache { get; set; }

    public List<float[]> Lightmap { get; set; } = [];
    public List<float> RawVertices { get; set; } = [];

    public Dictionary<string, Joint> AnimationJoints { get; set; } = [];

    /// <summary>
    /// Generate a table row of this model
    /// </summary>
    /// <returns>Table row containing information about the model, including name, address, scale, offset, texture and vertex count</returns>
    public string[] GetRow()
    {
        try
        {
            return
            [
                Name, Address.ToString("X"),
                $"{StaticUtils.DotFloatString(Scale[0])}x{StaticUtils.DotFloatString(Scale[1])}x{StaticUtils.DotFloatString(Scale[2])}",
                $"{StaticUtils.DotFloatString(Offset[0])}x{StaticUtils.DotFloatString(Offset[1])}x{StaticUtils.DotFloatString(Offset[2])}",
                Ascii.IsValid(Texture) ? Texture : "N/A",
                RawVertices.Count.ToString()
            ];
        }
        catch
        {
            return ["Error", "Error", "Error", "Error", "Error", "Error"];
        }
    }

    /// <summary>
    /// Once we figure out where the vertex data is, call this method to append vertices from the data and offset provided
    /// </summary>
    /// <param name="offset">Physical location of the vertex data (including the first 0x10 bytes that have the length)</param>
    /// <param name="data">LP4 binary data</param>
    public void AppendVerticies(int offset, byte[] data)
    {
        if ((offset >= data.Length) || (offset < 0)) return;
        var len = BitConverter.ToInt32(data, offset); // vertex count
        var nlen = BitConverter.ToInt32(data, offset + 4); // normal count
        var plen = BitConverter.ToInt32(data, offset + 8); // parameter count
        var uvlen = BitConverter.ToInt32(data, offset + 12); // UV count
        var texOffset = offset + (len * 0x10) + (nlen * 0x8) + (plen * 4) + 0x10;
        if (Debugger.IsAttached)
        {
            Console.WriteLine($"Debug: UV offset: 0x{texOffset:X}");
        }

        var uvOffset = texOffset;
        var comp = -1;
        var mask = 0x01;
        var matchId = 0;
        var modelBounds = offset + len * 0x10;
        var normalIdx = 0;
        bool sw = false;
        var partIdx = StaticUtils.AlternateNormals ? 0 : 1;
        for (var j = offset + 0x10; j < offset + (Math.Max(len, uvlen)) * 0x10 - 0x10; j += 0x10)
        {
            var x1 = BitConverter.ToSingle(data.Skip(j).Take(4).ToArray(), 0);
            var y1 = BitConverter.ToSingle(data.Skip(j + 0x4).Take(4).ToArray(), 0);
            var z1 = BitConverter.ToSingle(data.Skip(j + 0x8).Take(4).ToArray(), 0);
            var x2 = BitConverter.ToSingle(data.Skip(j + 0x10).Take(4).ToArray(), 0);
            var y2 = BitConverter.ToSingle(data.Skip(j + 0x14).Take(4).ToArray(), 0);
            var z2 = BitConverter.ToSingle(data.Skip(j + 0x18).Take(4).ToArray(), 0);
            var x3 = BitConverter.ToSingle(data.Skip(j + 0x20).Take(4).ToArray(), 0);
            var y3 = BitConverter.ToSingle(data.Skip(j + 0x24).Take(4).ToArray(), 0);
            var z3 = BitConverter.ToSingle(data.Skip(j + 0x28).Take(4).ToArray(), 0);

            if (j >= modelBounds)
            {
                x1 = BitConverter.ToSingle(data.Skip(j - modelBounds).Take(4).ToArray(), 0);
                y1 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x4).Take(4).ToArray(), 0);
                z1 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x8).Take(4).ToArray(), 0);
                x2 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x10).Take(4).ToArray(), 0);
                y2 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x14).Take(4).ToArray(), 0);
                z2 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x18).Take(4).ToArray(), 0);
                x3 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x20).Take(4).ToArray(), 0);
                y3 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x24).Take(4).ToArray(), 0);
                z3 = BitConverter.ToSingle(data.Skip(j - modelBounds + 0x28).Take(4).ToArray(), 0);
            }

            var mul = partIdx % 2 == 0 ? 1 : -1;

            RawVertices.AddRange(DecodeCoords(data.Skip(uvOffset).Take(8).ToArray()));
            RawVertices.Add(x1);
            RawVertices.Add(y1);
            RawVertices.Add(z1);
            RawVertices.AddRange(DecodeNormals(
                data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 0))).Take(8).ToArray(),
                StaticUtils.GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
            if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V1 {j:X}/{j + 4:X}/{j + 8:X}");

            RawVertices.AddRange(DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray()));
            RawVertices.Add(x2);
            RawVertices.Add(y2);
            RawVertices.Add(z2);
            RawVertices.AddRange(DecodeNormals(
                data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 1))).Take(8).ToArray(),
                StaticUtils.GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
            if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V2 {j + 0x10:X}/{j + 0x14:X}/{j + 0x18:X}");

            RawVertices.AddRange(DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray()));
            RawVertices.Add(x3);
            RawVertices.Add(y3);
            RawVertices.Add(z3);
            RawVertices.AddRange(DecodeNormals(
                data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 2))).Take(8).ToArray(),
                StaticUtils.GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
            if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V3 {j + 0x20:X}/{j + 0x24:X}/{j + 0x28:X}");

            //
            // let's define a comparison variable x (comp)
            // if x is -1, then set x to the value of UvFlags of the first point (pattern)
            //
            // let's define a variable y (pattern2)
            // if y XOR x is x, then the next point is located at position + 0x30
            // this also resets x to -1
            //
            var pattern = StaticUtils.GetUInt16(data.Skip(uvOffset + 6).Take(2).ToArray(), 0);
            if ((comp == -1) && ((pattern & mask) != mask))
            {
                comp = pattern;
            }

            var pattern2 = StaticUtils.GetUInt16(data.Skip(uvOffset + 24 + 6).Take(2).ToArray(), 0);

            partIdx++;
            if ((pattern2 & comp) == comp)
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine(
                        $"u16 splitA{matchId} @0x{uvOffset + 6:X};\nu16 splitB{matchId} @0x{uvOffset + 24 + 6:X};\n");
                }

                j += 0x20;
                uvOffset += 24;
                comp = -1;
                if (nlen % 0x20 / 0x10 == 0x10)
                {
                    partIdx = (((pattern2 & 0x01) != 0x01)) ? 0 : 1;
                }
                else
                {
                    partIdx = (((pattern2 & 0x01) != 0x01)) ? 1 : 0;
                }

                normalIdx += 3;
                continue;
            }

            uvOffset += 8;
            normalIdx += 1;
        }
    }

    /// <summary>
    /// Extract UV coordinates from the 8 bytes provided
    /// </summary>
    /// <param name="data">8 byte chunk containing the UV coordinate</param>
    /// <returns>X and Y coordinates</returns>
    public static float[] DecodeCoords(byte[] data)
    {
        // at +0x6h is the UV flags value, it describes how vertices should be parsed
        // explanation in Model.AppendVertices
        var div = BitConverter.ToInt16(data.Skip(4).Take(2).ToArray(), 0);
        var fx = BitConverter.ToInt16(data.Take(2).ToArray(), 0);
        var fy = BitConverter.ToInt16(data.Skip(2).Take(2).ToArray(), 0);
        return [(float)fx/div, -(float)fy/div]; // invert, because otherwise it's upside-down
    }

    /// <summary>
    /// Extract normal coordinates from the 8 bytes provided 
    /// </summary>
    /// <param name="data">8 byte chunk containing the normal coordinate</param>
    /// <returns>X, Y and Z coordinates</returns>
    public static float[] DecodeNormals(byte[] data, short div, int mul)
    {
        var x =  mul * BitConverter.ToInt16(data.Take(2).ToArray(), 0) / (float)div;
        var y =  mul * BitConverter.ToInt16(data.Skip(2).Take(2).ToArray(), 0) / (float)div;
        var z =  mul * BitConverter.ToInt16(data.Skip(4).Take(2).ToArray(), 0) / (float)div;
        return [z, y, x];
    }
}