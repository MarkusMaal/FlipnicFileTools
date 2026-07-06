using FlipnicLib.Formats;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace FlipnicLib.Types
{
    public class LayoutChunk : FormatBase
    {
        /// <summary>
        /// Name of the layout chunk used internally by the game
        /// </summary>
        public string? Name { get; set; }

        [JsonIgnore]
        public float UnkFloat { get; set; }

        public Properties[]? ModelProperties { get; set; }

        /// <summary>
        /// Defines a box of the model that can collide with other models.
        /// </summary>
        public Lp4Test.Vec4[]? Hitbox { get; set; }


        /// <summary>
        /// Information about the layout chunk
        /// </summary>
        public LayoutHeader LayoutChunkHeader { get; set; }

        public VertexProperties ModelVertexProperties { get; set; }

        public RawModel Model { get; set; }

        public JointIndexArr[]? Indices { get; set; }


        public LayoutChunk()
        {

        }

        public LayoutChunk(Stream data, int timelineCount)
        {
            var dataBuffer = new byte[0x20];
            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
            LayoutChunkHeader = new LayoutHeader()
            {
                ChunkCount = GetInt32(dataBuffer, 0),
                LayoutCount = GetInt32(dataBuffer, 4),
                Padding = GetInt32(dataBuffer, 8),
                HasHitbox = dataBuffer[12] > 0,
                UnkCount = GetInt32(dataBuffer, 16),
                UnkRabbit = GetInt32(dataBuffer, 20),
                EndPadding = GetInt64(dataBuffer, 24),
            };
            dataBuffer = new byte[0x20];
            if (data.Position > data.Length - dataBuffer.Length)
            {
                data.Position = data.Length;
                return;
            }
            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
            Name = GetString(dataBuffer);
            UnkFloat = GetFloat(dataBuffer, 0x1C);
            ModelProperties = new Properties[timelineCount];
            for (var k = 0; k < timelineCount; k++)
            {
                LayoutHeader? lH = LayoutChunkHeader;
                dataBuffer = new byte[128];
                data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                if (GetStringAt(dataBuffer, 0x20).Length > 4)
                {
                    data.Position -= 128; // we just read the name for the next chunk, jump back and do not continue past this point
                    return;
                }

                if (dataBuffer[0x23] != 0x00 && dataBuffer[0x27] != 0x00)
                {
                    data.Position -= 0x40; // similar to the last if statement, but with a different alignment
                    return;
                }
                var skews = new Lp4Test.Vec4[3];
                for (var i = 0; i <  skews.Length; i++)
                {
                    skews[i].X = GetFloat(dataBuffer, 0x30 + (i * 0x10));
                    skews[i].Y = GetFloat(dataBuffer, 0x30 + (i * 0x10) + 4);
                    skews[i].Z = GetFloat(dataBuffer, 0x30 + (i * 0x10) + 8);
                    skews[i].W = GetFloat(dataBuffer, 0x30 + (i * 0x10) + 12);
                }
                ModelProperties[k] = new Properties()
                {
                    PropertiesLayoutHeader = lH,
                    KeyFrameCount = GetInt32(dataBuffer, 0x0),
                    IsAnimated = dataBuffer[0x4] > 0,
                    HasUnknownSection = GetInt32(dataBuffer, 0x8) > 0,
                    UnkKitten = dataBuffer[0xC] > 0,
                    MorePadding = GetInt32(dataBuffer, 0x10),
                    JointCount = GetInt32(dataBuffer, 0x14),
                    HasAnotherUnknownSection = GetInt32(dataBuffer, 0x18),
                    UnkFly = GetInt32(dataBuffer, 0x1C) > 0,
                    HasLightmap = dataBuffer[0x20] > 0,
                    HasAlphaSequence = GetUInt32(dataBuffer, 0x24) > 0,
                    LightmapDataCount = GetInt32(dataBuffer, 0x28),
                    EvenMorePadding = GetInt32(dataBuffer, 0x2C),
                    ModelSkewing = skews,
                    ModelOffset = new Lp4Test.Vec4
                    {
                        X = GetFloat(dataBuffer, 0x60),
                        Y = GetFloat(dataBuffer, 0x64),
                        Z = GetFloat(dataBuffer, 0x68),
                        W = GetFloat(dataBuffer, 0x6C),
                    },
                    UnkSnake = GetInt32(dataBuffer, 0x70),
                    YetMorePadding = GetInt32(dataBuffer, 0x74),
                    UnkWhale = GetInt32(dataBuffer, 0x78),
                    UnkLastFloat = GetFloat(dataBuffer, 0x7C),
                };
                if (ModelProperties[k].KeyFrameCount < 100000)
                {
                    if (ModelProperties[k].IsAnimated && ModelProperties[k].KeyFrameCount > 0)
                    {
                        ModelProperties[k].AnimationSequence = new AnimationSequenceFrame[ModelProperties[k].KeyFrameCount];
                        for (var i = 0; i < ModelProperties[k].AnimationSequence.Length; i++)
                        {
                            dataBuffer = new byte[0x30];
                            data.ReadExactly(dataBuffer);
                            ModelProperties[k].AnimationSequence[i] = new AnimationSequenceFrame
                            {
                                Sx = GetFloat(dataBuffer, 0),
                                Sy = GetFloat(dataBuffer, 4),
                                Sz = GetFloat(dataBuffer, 8),
                                Sw = GetFloat(dataBuffer, 12),
                                UnkAlpaca = GetFloat(dataBuffer, 16),
                                UnkBeetle = GetFloat(dataBuffer, 20),
                                UnkCrow = GetFloat(dataBuffer, 24),
                                UnkDinosaur = GetFloat(dataBuffer, 28),
                                X = GetFloat(dataBuffer, 32),
                                Y = GetFloat(dataBuffer, 36),
                                Z = GetFloat(dataBuffer, 40),
                                W = GetFloat(dataBuffer, 44)
                            };
                        }
                    }


                    if (ModelProperties[k].JointCount < 10000 && ModelProperties[k].JointCount > 0)
                    {
                        ModelProperties[k].Joints = new Joint[ModelProperties[k].JointCount];
                        for (var i = 0; i < ModelProperties[k].Joints.Length; i++)
                        {
                            dataBuffer = new byte[0x60];
                            if (data.Position >= data.Length - dataBuffer.Length)
                            {
                                ModelProperties = null;
                                Console.ForegroundColor = ConsoleColor.Red;
                                throw new IndexOutOfRangeException("JOINTS");
                            }
                            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                            skews = new Lp4Test.Vec4[3];
                            for (var j = 0; j < skews.Length; j++)
                            {
                                skews[j].X = GetFloat(dataBuffer, 0x20 + (j * 0x10));
                                skews[j].Y = GetFloat(dataBuffer, 0x20 + (j * 0x10) + 4);
                                skews[j].Z = GetFloat(dataBuffer, 0x20 + (j * 0x10) + 8);
                                skews[j].W = GetFloat(dataBuffer, 0x20 + (j * 0x10) + 12);
                            }
                            ModelProperties[k].Joints[i] = new Joint
                            {
                                Name = GetString(dataBuffer),
                                JointSkewing = skews,
                                JointOffset = new Lp4Test.Vec4
                                {
                                    X = GetFloat(dataBuffer, 0x50),
                                    Y = GetFloat(dataBuffer, 0x54),
                                    Z = GetFloat(dataBuffer, 0x58),
                                    W = GetFloat(dataBuffer, 0x5C),
                                }
                            };
                        }
                    }

                    if (ModelProperties[k].HasUnknownSection && ModelProperties[k].KeyFrameCount > 0)
                    {
                        ModelProperties[k].UnknownSection = new Lp4Test.Vec4[ModelProperties[k].KeyFrameCount];
                        for (var i = 0; i < ModelProperties[k].UnknownSection.Length; i++)
                        {
                            dataBuffer = new byte[16];
                            data.ReadExactly(dataBuffer);
                            ModelProperties[k].UnknownSection[i] = new Lp4Test.Vec4
                            {
                                X = GetFloat(dataBuffer, 0),
                                Y = GetFloat(dataBuffer, 4),
                                Z = GetFloat(dataBuffer, 8),
                                W = GetFloat(dataBuffer, 12),
                            };
                        }
                    }

                    if (ModelProperties[k].HasAnotherUnknownSection > 0 && ModelProperties[k].KeyFrameCount > 0)
                    {
                        ModelProperties[k].AnotherUnknownSection = new Lp4Test.Vec4[ModelProperties[k].KeyFrameCount];
                        for (var i = 0; i < ModelProperties[k].AnotherUnknownSection.Length; i++)
                        {
                            dataBuffer = new byte[16];
                            data.ReadExactly(dataBuffer);
                            ModelProperties[k].AnotherUnknownSection[i] = new Lp4Test.Vec4
                            {
                                X = GetFloat(dataBuffer, 0),
                                Y = GetFloat(dataBuffer, 4),
                                Z = GetFloat(dataBuffer, 8),
                                W = GetFloat(dataBuffer, 12),
                            };
                        }
                    }


                    if (ModelProperties[k].HasAlphaSequence && ModelProperties[k].KeyFrameCount > 0)
                    {
                        ModelProperties[k].AlphaSequence = new Lp4Test.Vec4[ModelProperties[k].KeyFrameCount * ModelProperties[k].LightmapDataCount];
                        for (var i = 0; i < ModelProperties[k].AlphaSequence.Length; i++)
                        {
                            dataBuffer = new byte[16];
                            if (data.Position >= data.Length - dataBuffer.Length)
                            {
                                ModelProperties = null;
                                throw new IndexOutOfRangeException("ALPHA");
                            }
                            data.ReadExactly(dataBuffer);
                            ModelProperties[k].AlphaSequence[i] = new Lp4Test.Vec4
                            {
                                X = GetFloat(dataBuffer, 0),
                                Y = GetFloat(dataBuffer, 4),
                                Z = GetFloat(dataBuffer, 8),
                                W = GetFloat(dataBuffer, 12),
                            };
                        }
                    }

                    if (ModelProperties[k].HasLightmap && ModelProperties[k].LightmapDataCount > 0)
                    {
                        ModelProperties[k].Lightmap = new Lp4Test.Vec4[ModelProperties[k].LightmapDataCount];
                        for (var i = 0; i < ModelProperties[k].Lightmap.Length; i++)
                        {
                            dataBuffer = new byte[16];
                            if (data.Position >= data.Length - dataBuffer.Length)
                            {
                                ModelProperties = null;
                                throw new IndexOutOfRangeException("LIGHTMAP");
                            }
                            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                            ModelProperties[k].Lightmap[i] = new Lp4Test.Vec4
                            {
                                X = GetFloat(dataBuffer, 0),
                                Y = GetFloat(dataBuffer, 4),
                                Z = GetFloat(dataBuffer, 8),
                                W = GetFloat(dataBuffer, 12),
                            };
                        }
                    }

                }
            }
            if (LayoutChunkHeader.HasHitbox)
            {
                Hitbox = new Lp4Test.Vec4[8];
                for (var i = 0; i < Hitbox.Length; i++)
                {
                    dataBuffer = new byte[16];
                    if (data.Position >= data.Length - 0x10)
                    {
                        return;
                    }
                    data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
                    Hitbox[i] = new Lp4Test.Vec4
                    {
                        X = GetFloat(dataBuffer, 0),
                        Y = GetFloat(dataBuffer, 4),
                        Z = GetFloat(dataBuffer, 8),
                        W = GetFloat(dataBuffer, 12),
                    };
                }
                dataBuffer = new byte[0x20];
                data.ReadExactly(dataBuffer);
                ModelVertexProperties = new VertexProperties
                {
                    MaterialCount = GetInt32(dataBuffer, 0),
                    Multiplier = GetInt32(dataBuffer, 4),
                    Padding = GetInt64(dataBuffer, 8),
                    UnkRat = GetInt32(dataBuffer, 16),
                    UnkMouse = GetInt32(dataBuffer, 20),
                    AdDataCount = GetInt32(dataBuffer, 24),
                    AnimIndicesCount = GetInt32(dataBuffer, 28),
                    Materials = new Material[GetInt32(dataBuffer, 0)]
                };

                if (ModelVertexProperties.AdDataCount > 0)
                {
                    data.Position += 0x10 * ModelVertexProperties.AdDataCount * Math.Max(Math.Max(ModelVertexProperties.UnkMouse, ModelVertexProperties.UnkRat), 1);
                }

                if (ModelVertexProperties.AnimIndicesCount > 0 && ModelVertexProperties.AnimIndicesCount < 10000)
                {
                    dataBuffer = new byte[0x10];
                    data.ReadExactly(dataBuffer);
                    var nIndexArray = GetInt32(dataBuffer, 0);
                    Indices = new JointIndexArr[nIndexArray];
                    for (var i = 0; i < nIndexArray; i++)
                    {
                        dataBuffer = new byte[0x20];
                        data.ReadExactly(dataBuffer);
                        var name = GetString(dataBuffer);
                        dataBuffer = new byte[0x10];
                        data.ReadExactly(dataBuffer);
                        var arrayLength = GetInt32(dataBuffer, 0);
                        JointIndex[] indices = new JointIndex[arrayLength];
                        for (var j = 0; j < arrayLength; j++)
                        {
                            dataBuffer = new byte[0x10];
                            data.ReadExactly(dataBuffer);
                            indices[j] = new JointIndex
                            {
                                Index = GetUInt32(dataBuffer, 0),
                                UnkFloat = GetFloat(dataBuffer, 4),
                                Padding = GetInt64(dataBuffer, 8)
                            };
                        }
                        Indices[i] = new JointIndexArr
                        {
                            Indices = indices,
                            JointName = name
                        };
                    }
                }

                dataBuffer = new byte[0x10];
                data.ReadExactly(dataBuffer);
                var vertexCount = GetUInt32(dataBuffer, 0) * ModelVertexProperties.Multiplier;
                var normalCount = GetUInt32(dataBuffer, 4) * ModelVertexProperties.Multiplier;
                var colorCount = GetUInt32(dataBuffer, 8);
                var uvCount = GetUInt32(dataBuffer, 12);
                var limSigned32 = Math.Pow(2, 31);
                RawModel rm = new RawModel
                {
                    Vertices = new Lp4Test.Vec4[vertexCount < limSigned32 ? vertexCount : 0],
                    Normals = new Normal[normalCount < limSigned32 ? normalCount : 0],
                    Pixels = new Pixel[colorCount < limSigned32 ? colorCount : 0],
                    UVs = new UV[uvCount < limSigned32 ? uvCount : 0],
                };

                Model = rm;
                try
                {
                    dataBuffer = new byte[vertexCount * 0x10];
                    data.ReadExactly(dataBuffer);
                    for (var i = 0; i < vertexCount; i++)
                    {
                        var offset = i * 0x10;
                        rm.Vertices[i] = new Lp4Test.Vec4
                        {
                            X = GetFloat(dataBuffer, offset),
                            Y = GetFloat(dataBuffer, offset + 4),
                            Z = GetFloat(dataBuffer, offset + 8),
                            W = GetFloat(dataBuffer, offset + 12),
                        };
                    }
                    dataBuffer = new byte[normalCount * 0x8];
                    data.ReadExactly(dataBuffer);
                    for (var i = 0; i < normalCount; i++)
                    {
                        var offset = i * 0x8;
                        rm.Normals[i] = new Normal
                        {
                            X = GetInt16(dataBuffer, offset),
                            Y = GetInt16(dataBuffer, offset + 2),
                            Z = GetInt16(dataBuffer, offset + 4),
                            W = GetInt16(dataBuffer, offset + 6),
                        };
                    }
                    dataBuffer = new byte[colorCount * 0x4];
                    if (data.Position >= data.Length - dataBuffer.Length && Debugger.IsAttached) { throw new IndexOutOfRangeException("COLORS"); }
                    data.ReadExactly(dataBuffer);
                    for (var i = 0; i < colorCount * 4; i += 4)
                    {
                        rm.Pixels[i / 4] = new Pixel
                        {
                            R = dataBuffer[i],
                            G = dataBuffer[i+1],
                            B = dataBuffer[i+2],
                            A = dataBuffer[i+3],
                        };
                    }
                    dataBuffer = new byte[uvCount * 0x8];
                    data.ReadExactly(dataBuffer);
                    for (var i = 0; i < uvCount; i++)
                    {
                        var offset = i * 0x8;
                        var allFlags = GetInt16(dataBuffer, offset + 6);
                        rm.UVs[i] = new UV
                        {
                            X = GetInt16(dataBuffer, offset),
                            Y = GetInt16(dataBuffer, offset + 2),
                            Divider = GetInt16(dataBuffer, offset + 4),
                            DuplicationFlagA = (allFlags & 0x8000) != 0,
                            DuplicationFlagB = (allFlags & 0x1) != 0,
                        };
                    }
                } catch when (!Debugger.IsAttached)
                {
                    throw new IndexOutOfRangeException();
                }

                if (ModelVertexProperties.MaterialCount > 0)
                {
                    for (var i = 0; i < ModelVertexProperties.MaterialCount; i++)
                    {
                        dataBuffer = new byte[0xB0];
                        if (data.Position > -dataBuffer.Length + data.Length)
                        {
                            data.Position = data.Length;
                            return;
                        }
                        data.ReadExactly(dataBuffer);
                        ModelVertexProperties.Materials[i].Name = GetString(dataBuffer);
                        ModelVertexProperties.Materials[i].ContainsAdditionalSection = GetInt32(dataBuffer, 0x48) == 1;
                        ModelVertexProperties.Materials[i].TextureFile = GetStringAt(dataBuffer, 0x80);
                        /*if (ModelVertexProperties.Materials[i].ContainsAdditionalSection)
                        {
                            // TODO: make sense of this section
                            data.Position += 0x100;
                        }*/
                    }
                }
            }
            if (LayoutChunkHeader.UnkCount > 0) // maybe uncompressed vertices?
            {
                data.Position += 0xB0;
                dataBuffer = new byte[0x10];
                List<Lp4Test.Vec4> UncompressedVertices = [];
                data.ReadExactly(dataBuffer);
                var uncompressedVertexCount = GetInt32(dataBuffer, 0);
                for (var i = 0; i < uncompressedVertexCount * 2; i++)
                {
                    dataBuffer = new byte[0x10];
                    if (data.Position > data.Length - 0x10)
                    {
                        return;
                    }
                    data.ReadExactly(dataBuffer);
                    Lp4Test.Vec4 vec4 = new()
                    {
                        X = GetFloat(dataBuffer, 0),
                        Y = GetFloat(dataBuffer, 4),
                        Z = GetFloat(dataBuffer, 8),
                        W = GetFloat(dataBuffer, 12),
                    };
                    UncompressedVertices.Add(vec4);
                }
                Model = new RawModel
                {
                    Vertices = [.. UncompressedVertices]
                };
            }
            dataBuffer = new byte[0x20];
            if (data.Position > data.Length - 0x20)
            {
                return;
            }
            data.ReadExactly(dataBuffer);
            if (GetStringAt(dataBuffer, 0x10).Length > 4) // oops, we accidentally read the next layout chunk header, undo that real quick :/
            {
                data.Position -= 0x20;
            }
            if (ModelVertexProperties.MaterialCount <= 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
            } else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            if (!Ascii.IsValid(Name))
            {
                throw new FormatException();
            }
            if (Debugger.IsAttached) Console.Write(" " + (ModelVertexProperties.MaterialCount > 0 ? string.Join(",", ModelVertexProperties.Materials.Select(p => p.Name + ":" + p.TextureFile)) : Name));
            Console.ResetColor();
        }

        /// <summary>
        /// Header for the 3D model section
        /// </summary>
        public struct VertexProperties
        {
            public int MaterialCount { get; set; }
            public int Multiplier { get; set; }

            [JsonIgnore]
            public long Padding { get; set; }


            [JsonIgnore]
            public int UnkRat { get; set; }

            [JsonIgnore]
            public int UnkMouse { get; set; }
            public int AdDataCount { get; set; }
            public int AnimIndicesCount { get; set; }

            public JointIndexArr AnimIndices { get; set; }
            public RawModel Model { get; set; }
            public Material[] Materials { get; set; }
        }

        /// <summary>
        /// Material to use for this model. Sometimes this also contains
        /// a texture filename.
        /// </summary>
        public struct Material
        {
            public string Name { get; set; }

            public string TextureFile { get; set; }

            public bool ContainsAdditionalSection { get; set; }
        }

        /// <summary>
        /// The actual 3D model data without the model section header.
        /// </summary>
        public struct RawModel
        {
            public readonly int VertexCount => Vertices?.Length ?? 0;
            public readonly int NormalCount => Normals?.Length ?? 0;
            public readonly int ColorCount => Pixels?.Length ?? 0;
            public readonly int UvCount => UVs?.Length ?? 0;

            public Lp4Test.Vec4[] Vertices { get; set; }
            public Normal[] Normals { get; set; }
            public Pixel[] Pixels { get; set; }
            public UV[] UVs { get; set; }
        }

        /// <summary>
        /// Defines points of the model that can be indipendently animated.
        /// These points don't appear to use UV compression.
        /// </summary>
        public struct JointIndexArr
        {
            public string JointName { get; set; }
            public JointIndex[] Indices { get; set; }
            public readonly uint IndexCount => (uint)(Indices?.Length ?? 0);
        };

        /// <summary>
        /// When a model doesn't have a texture material, it may use embedded pixels
        /// instead. These correspond to each vertex.
        /// </summary>
        public struct Pixel
        {
            public byte R { get; set; }

            public byte G { get; set; }

            public byte B { get; set; }

            public byte A { get; set; }
        }

        /// <summary>
        /// Index within the compressed array of vertices
        /// </summary>
        public struct JointIndex
        {
            public uint Index { get; set; }

            [JsonIgnore]
            public float UnkFloat { get; set; }

            [JsonIgnore]
            public long Padding { get; set; }
        }

        /// <summary>
        /// Defines the initial state of the joint (similar to a part of model properties)
        /// </summary>
        public struct Joint
        {
            public string Name { get; set; }
            public Lp4Test.Vec4[] JointSkewing { get; set; }
            public Lp4Test.Vec4 JointOffset { get; set; }
        }

        /// <summary>
        /// Defines the structure and properties of the section
        /// </summary>
        public struct Properties
        {
            public LayoutHeader? PropertiesLayoutHeader { get; set; }

            public int KeyFrameCount { get; set; }
            public bool IsAnimated { get; set; }
            public bool HasUnknownSection { get; set; }

            [JsonIgnore]
            public bool UnkKitten { get; set; }

            [JsonIgnore]
            public int MorePadding { get; set; }
            public int JointCount { get; set; }
            public int HasAnotherUnknownSection { get; set; }

            [JsonIgnore]
            public bool UnkFly { get; set; }
            public bool HasLightmap { get; set; }
            public bool HasAlphaSequence { get; set; }
            public int LightmapDataCount { get; set; }

            public int EvenMorePadding { get; set; }

            public Lp4Test.Vec4[] ModelSkewing { get; set; }
            public Lp4Test.Vec4 ModelOffset { get; set; }

            [JsonIgnore]
            public int UnkSnake { get; set; }

            [JsonIgnore]
            public int YetMorePadding { get; set; }

            [JsonIgnore]
            public int UnkWhale { get; set; }

            [JsonIgnore]
            public float UnkLastFloat { get; set; }

            public Joint[] Joints { get; set; }
            public AnimationSequenceFrame[] AnimationSequence { get; set; }
            public Lp4Test.Vec4[] AlphaSequence { get; set; }
            public Lp4Test.Vec4[] Lightmap { get; set; }
            public Lp4Test.Vec4[] UnknownSection { get; set; }
            public Lp4Test.Vec4[] AnotherUnknownSection { get; set; }
        }

        /// <summary>
        /// Defines normal vectors for each vertex (outer point)
        /// </summary>
        public struct Normal
        {
            public short X { get; set; }

            public short Y { get; set; }

            public short Z { get; set; }

            public short W { get; set; }
        }

        /// <summary>
        /// Defines the point inside the material to use for this vertex.
        /// X/Y values correspond to a coordinate within the texture material,
        /// which are divided by divider to get a fractional value.
        /// <br/>
        /// Duplication flags are related to how each polygon gets extracted
        /// from the points array.
        /// </summary>
        public struct UV
        {
            public short X { get; set; }

            public short Y { get; set; }

            public short Divider { get; set; }

            public bool DuplicationFlagA { get; set; }

            public bool DuplicationFlagB { get; set; }
        }

        /// <summary>
        /// Based on observations of 2D character animations.
        /// S-values define skewing and XYZW are the position.
        /// <br/>
        /// It may also be possible that all these values are just skews.
        /// </summary>
        public struct AnimationSequenceFrame
        {
            public float Sx { get; set; }

            public float Sy { get; set; }

            public float Sz { get; set; }

            public float Sw { get; set; }

            public float UnkAlpaca { get; set; }

            public float UnkBeetle { get; set; }

            public float UnkCrow { get; set; }

            public float UnkDinosaur { get; set; }

            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public float W { get; set; }
        }

        public struct LayoutHeader
        {
            public int ChunkCount { get; set; }
            public int LayoutCount { get; set; }

            [JsonIgnore]
            public int Padding { get; set; }

            /// <summary>
            /// Also specifies if this section has a model
            /// </summary>
            public bool HasHitbox { get; set; }

            [JsonIgnore]
            public int UnkCount { get; set; }

            [JsonIgnore]
            public int UnkRabbit { get; set; }

            [JsonIgnore]
            public long EndPadding { get; set; }
        }
    }
}
