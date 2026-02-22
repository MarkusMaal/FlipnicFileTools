using System.Diagnostics;

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

    public Tim2? Texture { get; set; }

    public List<Model> Models { get; set; } = [];
    
    public Model? SelectedModel { get; set; }

    public override string ToString()
    {
        var er = HasEmbeddedResources ? "Yes" : "No";
        var i2 = Is2dAnimation ? "Yes" : "No";
        var o = $"""
                Type: {Type.ToString()}
                Model count: {ModelOffsets.Count}
                Has embedded resources: {er}
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
        Texture = new Tim2(d, SelectedModel.Texture);
    }

    // this parser seems to fail most of the time, so further adjustments maybe needed
    private void GetModelOffset()
    {
        try
        {
            var additionalDataLength = StaticUtils.GetInt32(data, 8); 
            var i = 0xA0;
            i += additionalDataLength * 0x10;
            while ((i < data.Length) && (i >= 0))
            {
                var attributeSetCount = StaticUtils.GetInt32(data, i + 4);
                var extraUnknownDataCount = StaticUtils.GetInt32(data, i + 12);
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
                var animationJoints = StaticUtils.GetInt32(data, i + 0x24);
                if (animationJoints > 0) // this is an animated model, ignore the animation stuff and just get the default pose
                {
                    i += 0x10;
                    if (attributeSetCount > 65536)
                    {
                        StaticUtils.DecodeColors("~-CError~--: Attribute set count was too large, continuing would cause hangs! Parser was halted!\n");
                        OldMethod();
                        return;
                    }
                    for (var j = 0; j < attributeSetCount; j++) // joint definitions
                    {
                        animationJoints = StaticUtils.GetInt32(data, i + 0x14);
                        var extraDataLength =  StaticUtils.GetInt32(data, i + 0x28) - 1;
                        i += 0x90;
                        i += animationJoints * 0x60;
                        i +=  extraDataLength * 0x10;
                    }

                    i += 0x80;
                    
                    // model definition
                    var extraData = StaticUtils.GetInt32(data, i + 0x18);
                    i += 0x10 * extraData;
                    i += 0x20;

                    // animations
                    var numAnimations = StaticUtils.GetInt32(data, i);
                    i += 0x10;
                    if (numAnimations > 65536)
                    {
                        StaticUtils.DecodeColors("~-CError~--: Animation count was too large, continuing would cause hangs! Parser was halted!\n");
                        OldMethod();
                        return;
                    }
                    for (var j = 0; j < numAnimations - 1; j++)
                    {
                        i += 0x20; // animation name
                        var keyFrames = StaticUtils.GetInt32(data, i);
                        i += 0x10; // keyframe count
                        i += 0x10 * keyFrames; // keyframes
                    }
                    
                    i += 0x20; // animation name
                    i += 0x10; // keyframe count
                    i += 0x10 * StaticUtils.GetInt32(data, i-0x10); // keyframes
                    // model
                    var modelVertCount = StaticUtils.GetInt32(data, i);
                    var modelNormalCount = StaticUtils.GetInt32(data, i + 4);
                    var modelTextureCoordCount = StaticUtils.GetInt32(data, i + 12);
                    model.AppendVerticies(i, data);
                    i += 0x10 * modelVertCount;
                    i += 0x8 * modelNormalCount;
                    i += 0x8 * modelTextureCoordCount;
                    i += 0x90;
                    model.Texture = StaticUtils.GetStringAt(data, i);
                    Models.Add(model);
                    break;
                }
                i += 0x90 * attributeSetCount; // position, size, etc
                i += 0x90 * extraUnknownDataCount; // some unknown sections

                // model
                var padding = 0x10 * (StaticUtils.GetInt32(data, i + 0x18));
                i += 0x20; // model identifier, I guess?
                i += padding;
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


            if (Models.Count > 0)
            {
                SelectedModel = Models[0];
            }

            if (SelectedModel.RawVertices.Count != 0)
            {
                StaticUtils.DecodeColors("~-ASuccess~--: Successfully decoded the LP4 file!");
                return;
            }
            Models.Clear();
            OldMethod();
        }
        catch when (!Debugger.IsAttached)
        {
            OldMethod();
        }
    }
    
    private void OldMethod()
    {
        StaticUtils.DecodeColors("~-EWarning~--: failed to parse LP4 file correctly, falling back to brute-force method!\n");
        var i = 0;
        while (i < data.Length - 0x20)
        {
            int f2;
            float f, f3;
            short f4;
            f2 = BitConverter.ToInt32([.. data.Skip(i).Take(4)], 0);
            if (i + f2 * 0x10 >= data.Length - 0x20)
            {
                i += 0x10;
                continue;
            }
            f = BitConverter.ToSingle([.. data.Skip(i+0x1C).Take(4)], 0);
            f3 = BitConverter.ToSingle([.. data.Skip(i+f2*0x10+0xC).Take(4)]);
            f4 = BitConverter.ToInt16([.. data.Skip(i+f2*0x10+0x1E).Take(4)], 0);
            var endModelData = i + 0x10 + StaticUtils.GetInt32(data, i) * 10 + StaticUtils.GetInt32(data, i+4) * 8 + StaticUtils.GetInt32(data, i+8) * 4 +  StaticUtils.GetInt32(data, i+12) * 8;
            if ((f == 1.0f) && (f3 == 1.0f) && (f4 == 0 || StaticUtils.GetStringAt(data, endModelData).StartsWith("mat")))
            {
                var len = f2;
                if ((len > 0) && (len < data.Length) && i > 0x80)
                {
                    try
                    {
                        var tm = new Model();
                        tm.AppendVerticies(i, data);

                        rawVerticies = tm.RawVertices;
                        if (rawVerticies.Count > 0)
                        {
                            StaticUtils.DecodeColors($"~-ASuccess~--: Detected valid model data at offset 0x{i:X}\n");
                        }
                        else
                        {
                            StaticUtils.DecodeColors($"~-EWarning~--: Offset 0x{i:X} contains 0 vertices, continue searching...\n");
                            i += 0x10;
                            continue;
                        }
                        i += 2 * len * 0x10 + 0xA0;
                        break;
                    }
                    catch
                    {
                        StaticUtils.DecodeColors($"~-CError~--: Attempt to read from offset 0x{i:X} threw an error, continue searching...\n");
                    }
                }
            }
            i += 0x10;
        }

        if (rawVerticies.Count == 0)
        {
            StaticUtils.DecodeColors("~-CError~--: No model data found\n");
            return;
        }
        TexturePath = StaticUtils.GetString(data.Skip(i).Take(0x20).ToArray());

        if (!File.Exists(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
                TexturePath.ToUpper())))
        {
            StaticUtils.DecodeColors("~-EWarning~--: The model does not have a texture\n");
            return;
        }
        var fs = File.OpenRead(Path.Combine(new FileInfo(FileName).Directory?.FullName ?? "/",
            TexturePath.ToUpper()));
        var d = new byte[fs.Length];
        fs.ReadExactly(d, 0, d.Length);
        Texture = new Tim2(d, TexturePath);
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
            Texture = new Tim2(d, SelectedModel.Texture);
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

public class Model
{
    public string Name { get; set; }
    public string Texture { get; set; }
    
    public float[] Scale { get; set; }
    public float[] Offset { get; set; }
    public int Address { get; set; }

    public List<float> RawVertices { get; set; } = [];

    /// <summary>
    /// Generate a table row of this model
    /// </summary>
    /// <returns>Table row containing information about the model, including name, address, scale, offset, texture and vertex count</returns>
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