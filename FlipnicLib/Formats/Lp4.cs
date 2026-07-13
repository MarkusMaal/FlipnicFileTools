using FlipnicLib.Types;
using Syroot.BinaryData;
using System.Diagnostics;
using System.Text.Json.Serialization;
using static FlipnicLib.Types.LayoutChunk;

namespace FlipnicLib.Formats
{
    public class Lp4 : FormatBase
    {
        /// <summary>
        /// Required. First 0x20 bytes of the LP4 file
        /// </summary>
        public Header FormatHeader { get; set; }

        /// <summary>
        /// Required. Sometimes a model may have multiple timelines that the game can choose between.
        /// There has to be at least 1 timeline and these are defined right after the
        /// format header. This also means the minimum size for a LP4 file is 48 bytes (1 header, 1 timeline).
        /// </summary>
        public Timeline[]? Timelines { get; set; }

        /// <summary>
        /// Optional. This is a box that defines global bounds for every model inside the LP4 file
        /// (for position calculation purposes).
        /// </summary>
        public Vec4[]? BoundingBox { get; set; }

        /// <summary>
        /// Optional. These define the actual models, animations, joints, etc. of a specific model.
        /// If these parts and bounding box are omitted, that likely means this file is used as a
        /// trigger for some kind of action (e.g. playing a sound effect).
        /// </summary>
        public List<LayoutChunk>? LayoutChunks { get; set; }
        
        [JsonIgnore]
        public LayoutChunk? SelectedModel { get; set; }
        
        [JsonIgnore]
        public byte[]? CachedTexture { get; set; }
        
        [JsonIgnore]
        public string? FilePath { get; set; }

        /// <summary>
        /// This constructor is present for serialization purposes
        /// </summary>
        public Lp4()
        {

        }

        public Lp4(FileStream data) : this((Stream)data)
        {
            FilePath = data.Name;
        }
        
        /// <summary>
        /// Opens and processes a LP4 file
        /// </summary>
        /// <param name="data">Stream containing the LP4 data (any class that extends Stream is also accepted here, e.g. FileStream)</param>
        public Lp4(Stream data)
        {
            var dataBuffer = new byte[32];
            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
            FormatHeader = new Header()
            {
                HeaderSize = GetInt32(dataBuffer, 0),
                HasLayouts = dataBuffer[4] > 0,
                LayoutChunkPropertiesCount = GetInt32(dataBuffer, 8),
                TimelineCount = GetInt32(dataBuffer, 12),
                UnkFlamingo = dataBuffer[16],
                HasBoundingBox = dataBuffer[17] > 0,
                Padding = GetInt16(dataBuffer, 18),
                UnkLion = GetFloat(dataBuffer, 20),
                EndPadding = GetInt64(dataBuffer, 24)
            };
            Timelines = new Timeline[FormatHeader.TimelineCount];
            BoundingBox = FormatHeader.HasBoundingBox ? new Vec4[8] : [];

            for (var i = 0; i < Timelines.Length; i++)
            {
                dataBuffer = new byte[16];
                data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                Timelines[i] = new Timeline
                {
                    FrameCountA = GetInt32(dataBuffer, 0),
                    FrameCountB = GetInt32(dataBuffer, 4),
                    UnkBear = GetInt32(dataBuffer, 8),
                    LoopingEnable = dataBuffer[12] > 0,
                };
            }

            for (var i = 0; i < BoundingBox.Length; i++)
            {
                dataBuffer = new byte[16];
                data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                BoundingBox[i] = new Vec4
                {
                    X = GetFloat(dataBuffer, 0),
                    Y = GetFloat(dataBuffer, 4),
                    Z = GetFloat(dataBuffer, 8),
                    W = GetFloat(dataBuffer, 12),
                };
            }
            if (FormatHeader.HasLayouts)
            {
                dataBuffer = new byte[32];
                data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                var firstLayoutHeader = new LayoutHeader
                {
                    ChunkCount = GetInt32(dataBuffer, 0),
                    LayoutCount = GetInt32(dataBuffer, 4),
                    Padding = GetInt32(dataBuffer, 8),
                    HasHitbox = dataBuffer[12] > 0,
                    UnkCount = GetInt32(dataBuffer, 16),
                    UnkRabbit = GetInt32(dataBuffer, 20),
                    UnkAlpaca = GetInt32(dataBuffer, 24),
                    MaterialsHaveAdditionalSection = GetInt32(dataBuffer, 28) == 1,
                };
                LayoutChunks = [];
                data.Position -= 32;
                var startPos = data.Position;
                if (firstLayoutHeader.LayoutCount > 0)
                {
                    var max = 1;
                    var i = 0;
                    while (data.Position < data.Length)
                    {
                        
                        LayoutChunks.Add(new LayoutChunk(data, FormatHeader.LayoutChunkPropertiesCount));
                        dataBuffer = new byte[32];
                        if (data.Position > data.Length - 0x20)
                        {
                            break;
                        }
                        data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                        if (GetString(dataBuffer).Length > 0)
                        {
                            data.Position -= 0x40;
                        } else
                        {
                            data.Position -= 0x20;
                        }
                        if (data.Position >= data.Length) break;
                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Extract an independently animated part of the model.
        /// </summary>
        /// <param name="joint">Index array to extract</param>
        /// <param name="inputModel">Full 3D model inside a layout chunk</param>
        /// <returns>Floats ready for rendering (Ux-Uy-Vx-Vy-Vz-Nx-Ny-Nz)</returns>
        public float[] DecodeIndices(JointIndexArr joint, RawModel inputModel)
        {
            var vertices = new List<Vec4>();
            var normals = new List<Normal>();
            var colors = new List<Pixel>();
            var uvs = new List<UV>();
            List<bool[]> pattern = [[false, true], [true, false], [true, true]];
            foreach (var p in joint.Indices)
            {
                if (inputModel.Vertices.Length > p.Index) vertices.Add(inputModel.Vertices[p.Index]);
                if (inputModel.Normals.Length > p.Index) normals.Add(inputModel.Normals[p.Index]);
                if (inputModel.Pixels.Length > p.Index) colors.Add(inputModel.Pixels[p.Index]);
                var first = pattern[0];
                if (inputModel.UVs.Length > p.Index)
                {
                    var uv = inputModel.UVs[p.Index];
                    var nUv = uv with { DuplicationFlagA = first[0], DuplicationFlagB = first[1] };
                    uvs.Add(nUv);
                }
                pattern.RemoveAt(0);
                pattern.Add(first);
            }
            return GetRawVertices(new RawModel()
            {
                Vertices = [.. vertices],
                Normals = [.. normals],
                Pixels = [.. colors],
                UVs = [.. uvs]
            });
        }

        /// <summary>
        /// Decode vertices for use with OpenTK or exporting as Wavefront OBJ
        /// </summary>
        /// <param name="inputModel">Full model stored inside a LayoutChunk</param>
        /// <returns>Floats ready for rendering (Ux-Uy-Vx-Vy-Vz-Nx-Ny-Nz)</returns>
        public float[] GetRawVertices(RawModel inputModel)
        {
            MemoryStream ms = new();

            ms.Write(BitConverter.GetBytes(inputModel.VertexCount));
            ms.Write(BitConverter.GetBytes(inputModel.NormalCount));
            ms.Write(BitConverter.GetBytes(inputModel.ColorCount));
            ms.Write(BitConverter.GetBytes(inputModel.UvCount));
            foreach (var v in inputModel.Vertices)
            {
                ms.Write(v.X);
                ms.Write(v.Y);
                ms.Write(v.Z);
                ms.Write(v.W);
            }
            foreach (var n in inputModel.Normals)
            {
                ms.Write(n.X);
                ms.Write(n.Y);
                ms.Write(n.Z);
                ms.Write(n.W);
            }
            foreach (var p in inputModel.Pixels)
            {
                ms.Write(p.R);
                ms.Write(p.G);
                ms.Write(p.B);
                ms.Write(p.A);
            }
            foreach (var u in inputModel.UVs)
            {
                ms.Write(u.X);
                ms.Write(u.Y);
                ms.Write(u.Divider);
                ms.WriteByte((byte)(u.DuplicationFlagB ? 0x01 : 0x0));
                ms.WriteByte((byte)(u.DuplicationFlagA ? 0x80 : 0x0));
            }

            for (var i = 0; i < 0x18; i++)
            {
                ms.WriteByte(0);
            }

            var msArray = ms.ToArray();
            ms.Close();

            return AppendVerticies(0, msArray);
        }

        /// <summary>
        /// Once we figure out where the vertex data is, call this method to append vertices from the data and offset provided
        /// </summary>
        /// <param name="offset">Physical location of the vertex data (including the first 0x10 bytes that have the length)</param>
        /// <param name="data">LP4 binary data</param>
        private float[] AppendVerticies(int offset, byte[] data)
        {
            // yes, this is jank, but it works, so I don't touch it
            var rawVertices = new List<float>();
            if ((offset >= data.Length) || (offset < 0)) return [];
            var len = BitConverter.ToInt32(data, offset); // vertex count
            var nlen = BitConverter.ToInt32(data, offset + 4); // normal count
            var clen = BitConverter.ToInt32(data, offset + 8); // color count
            var uvlen = BitConverter.ToInt32(data, offset + 12); // UV count
            var texOffset = offset + (len * 0x10) + (nlen * 0x8) + (clen * 4) + 0x10;
            var colOffset = offset + (len * 0x10) + (nlen * 0x8) + 0x10;
            if (Debugger.IsAttached)
            {
                Console.WriteLine($"Debug: UV offset: 0x{texOffset:X}");
            }

            var uvOffset = texOffset;
            var comp = -1;
            const int mask = 0x01;
            const int matchId = 0;
            var modelBounds = offset + len * 0x10;
            var normalIdx = 0;
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
                //if (colOffset < texOffset) { Colors.Add([data[colOffset], data[colOffset + 1], data[colOffset + 2], data[colOffset + 3]]); }
                rawVertices.AddRange(DecodeCoords(data.Skip(uvOffset).Take(8).ToArray()));
                rawVertices.Add(x1);
                rawVertices.Add(y1);
                rawVertices.Add(z1);
                rawVertices.AddRange(DecodeNormals(
                    data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 0))).Take(8).ToArray(),
                    GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
                if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V1 {j:X}/{j + 4:X}/{j + 8:X}");

                //if (colOffset < texOffset) { Colors.Add([data[colOffset], data[colOffset + 1], data[colOffset + 2], data[colOffset + 3]]); }
                rawVertices.AddRange(DecodeCoords(data.Skip(uvOffset + 8).Take(8).ToArray()));
                rawVertices.Add(x2);
                rawVertices.Add(y2);
                rawVertices.Add(z2);
                rawVertices.AddRange(DecodeNormals(
                    data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 1))).Take(8).ToArray(),
                    GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
                if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V2 {j + 0x10:X}/{j + 0x14:X}/{j + 0x18:X}");

                //if (colOffset < texOffset) { Colors.Add([data[colOffset], data[colOffset + 1], data[colOffset + 2], data[colOffset + 3]]); }
                rawVertices.AddRange(DecodeCoords(data.Skip(uvOffset + 16).Take(8).ToArray()));
                rawVertices.Add(x3);
                rawVertices.Add(y3);
                rawVertices.Add(z3);
                rawVertices.AddRange(DecodeNormals(
                    data.Skip(offset + len * 0x10 + 0x10 + (0x8 * (normalIdx + 2))).Take(8).ToArray(),
                    GetInt16(data.Skip(uvOffset + 4).Take(2).ToArray(), 0), mul));
                if (Debugger.IsAttached) Console.WriteLine($"Debug: Vertex V3 {j + 0x20:X}/{j + 0x24:X}/{j + 0x28:X}");

                //
                // let's define a comparison variable x (comp)
                // if x is -1, then set x to the value of UvFlags of the first point (pattern)
                //
                // let's define a variable y (pattern2)
                // if y XOR x is x, then the next point is located at position + 0x30
                // this also resets x to -1
                //
                var pattern = GetUInt16(data.Skip(uvOffset + 6).Take(2).ToArray(), 0);
                if ((comp == -1) && ((pattern & mask) != mask))
                {
                    comp = pattern;
                }
                var patData = data.Skip(uvOffset + 24 + 6).Take(2).ToArray();
                var pattern2 = GetUInt16(patData, 0);

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
                colOffset += 4;
            }
            return [.. rawVertices];
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
            return [(float)fx / div, -(float)fy / div]; // invert, because otherwise it's upside-down
        }

        /// <summary>
        /// Extract normal coordinates from the 8 bytes provided 
        /// </summary>
        /// <param name="data">8 byte chunk containing the normal coordinate</param>
        /// <param name="div">Value divider</param>
        /// <param name="mul">Value multiplier</param>
        /// <returns>X, Y and Z coordinates</returns>
        public static float[] DecodeNormals(byte[] data, short div, int mul)
        {
            var x = mul * BitConverter.ToInt16(data.Take(2).ToArray(), 0) / (float)div;
            var y = mul * BitConverter.ToInt16(data.Skip(2).Take(2).ToArray(), 0) / (float)div;
            var z = mul * BitConverter.ToInt16(data.Skip(4).Take(2).ToArray(), 0) / (float)div;
            return [z, y, x];
        }

        public float[] GetBoundingBox()
        {
            if (BoundingBox == null) return [];

            // basically the points in the file define the top and bottom side of the rectangle let's call these 0 1 2 3 4 5 6 7,
            // where 0 1 2 3 are the points of the first rectangle in a 3D space and 4 5 6 7 define the second rectangle
            //
            // with some very basic 3D geometry we can simply "connect the dots" to get the remaining triangles required to generate a full
            // box shape
            var staticIndices = new[]
            {
                0, 1, 2, 1, 2, 3, 4, 5, 6, 6, 7, 5, 2, 3, 6, 3, 7, 6, 0, 1, 5, 0, 5, 4, 2, 0, 4, 6, 4, 2, 1, 3, 7, 1, 5,
                7
            };
            var boundingBox = staticIndices.Select(i => (float[])[BoundingBox[i].X, BoundingBox[i].Y, BoundingBox[i].Z]).ToList();

            var floats = new List<float>();
            foreach (var vtx in boundingBox)
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

        public struct CharacterAnimation
        {
            public float[] Values { get; set; }
        }

        public struct Header
        {
            /// <summary>
            /// Seems to always be 2
            /// </summary>
            public int HeaderSize { get; set; }

            /// <summary>
            /// If false, no layout chunks are in the file
            /// </summary>
            public bool HasLayouts { get; set; }

            /// <summary>
            /// The number of timelines, which defines some other characteristics used elsewhere in the file
            /// </summary>
            public int TimelineCount { get; set; }

            public int LayoutChunkPropertiesCount { get; set; }

            [JsonIgnore]
            public byte UnkFlamingo { get; set; }

            /// <summary>
            /// If false, no bounding box will be generated (mainly done for 2D animations)
            /// </summary>
            public bool HasBoundingBox { get; set; }

            [JsonIgnore]
            public short Padding { get; set; }

            [JsonIgnore]
            public float UnkLion { get; set; }

            [JsonIgnore]
            public long EndPadding { get; set; }
        }

        public struct Timeline
        {
            /// <summary>
            /// Not really sure how this works yet
            /// </summary>
            public int FrameCountA { get; set; }

            /// <summary>
            /// Not always the same as FrameCountA
            /// </summary>
            public int FrameCountB { get; set; }

            [JsonIgnore]
            public int UnkBear { get; set; }

            /// <summary>
            /// If set to false, animations don't loop (useful for particles)
            /// </summary>
            public bool LoopingEnable { get; set; }
        }

        /// <summary>
        /// Defines part of the data that contains 4 float values
        /// </summary>
        public struct Vec4
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public float W { get; set; }
        }
        
        public override string ToString()
        {
            var er = FormatHeader.HasBoundingBox ? "Yes" : "No";
            var hl = FormatHeader.HasLayouts ? "Yes" : "No";
            FilePath ??= "";
            var o = $"""
                    3D model data ({new FileInfo(FilePath).Name})
                    
                    Has bounding box: {er}
                    Has layouts chunks: {hl}
                    Timelines: {FormatHeader.TimelineCount}
                    Layout chunk sections count: {FormatHeader.LayoutChunkPropertiesCount}
                    
                    """;
            string[] cols = ["X", "Y", "Z"];
            List<string[]> rows = [];
            rows.AddRange(BoundingBox?.Select(vertex => (string[])[DotFloatString(vertex.X), DotFloatString(vertex.Y), DotFloatString(vertex.Z)]) ?? []);
            if (FormatHeader.HasBoundingBox)
            {
                o += $"""

                      Bounding box:
                      {StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput)}

                      """;
            }

            rows.Clear();
            //if (Type != FileType.StaticModel) return o;
            if (!FormatHeader.HasLayouts) return o;

            o += $"""

                 Layout chunks:

                 
                 """;
            foreach (var layout in LayoutChunks ?? [])
            {
                var hb = layout.LayoutChunkHeader.HasHitbox ? "Yes" : "No";
                o += $"""
                      
                      Name: {layout.Name ?? "(null)"}
                      
                      Contains model: {hb}
                      Joint indices: {layout.Indices?.Length ?? 0}
                      
                      """;
                if (layout.Model != null)
                {
                    o += $"""
                          Materials: {layout.ModelVertexProperties.Materials.Length}
                          Vertices: {layout.Model.Value.VertexCount}
                          Normals: {layout.Model.Value.NormalCount}
                          Pixels: {layout.Model.Value.ColorCount}
                          UVs: {layout.Model.Value.UvCount}

                          """;
                }

                o += "\nProperties:\n";
                cols = ["Keyframes", "Lighting", "Light animation", "Vertex animation", "Joints"];
                rows.Clear();
                for (var i = 0; i < FormatHeader.LayoutChunkPropertiesCount; i++)
                {
                    var prop = layout.ModelProperties;
                    if (prop == null) continue;
                    rows.Add([
                        prop[i].KeyFrameCount.ToString(),
                        prop[i].HasLightmap ? $"Yes ({prop[i].LightmapDataCount})": "No",
                        prop[i].HasAlphaSequence ? $"Yes ({prop[i].AlphaSequence.Length} frames)" : "No", 
                        prop[i].IsAnimated ? "Yes" : "No",
                        prop[i].JointCount.ToString()
                        ]);
                }
                o += StaticUtils.GenerateTable(cols, rows, StaticUtils.SimpleOutput);
            }
            return o;
        }

    }

    [JsonSerializable(typeof(Lp4))]
    [JsonSerializable(typeof(LayoutChunk))]
    [JsonSourceGenerationOptions(WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
    public partial class Lp4TestGenerationContext : JsonSerializerContext
    {
    }
}