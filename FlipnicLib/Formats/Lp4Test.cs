using FlipnicLib.Formats;
using FlipnicLib.Types;
using System.Text.Json.Serialization;
using static FlipnicLib.Types.LayoutChunk;

namespace FlipnicLib.Formats
{
    public partial class Lp4Test : FormatBase
    {
        public Header FormatHeader { get; set; }
        public Timeline[]? Timelines { get; set; }

        public Vec4[]? BoundingBox { get; set; }

        public List<LayoutChunk>? LayoutChunks { get; set; }

        public Lp4Test()
        {

        }

        public Lp4Test(Stream data)
        {
            var dataBuffer = new byte[32];
            data.ReadExactly(dataBuffer, 0, dataBuffer.Length);
            FormatHeader = new Header()
            {
                HeaderSize = GetInt32(dataBuffer, 0),
                HasLayouts = dataBuffer[4] > 0,
                UnkLizard = GetInt32(dataBuffer, 8),
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
                    EndPadding = GetInt64(dataBuffer, 24),
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
                        
                        LayoutChunks.Add(new LayoutChunk(data, FormatHeader.TimelineCount));
                        if (LayoutChunks[^1].ModelVertexProperties.AnimIndicesCount > 0)
                        {
                            data.Position -= 0x20;
                            if (data.Position < data.Length) max += 1;
                            i++;
                            continue;
                        }
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
                        if (data.Position == startPos)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write(" not read ");
                            Console.ResetColor();
                            break;
                        }
                        if (LayoutChunks[i].ModelProperties[0].JointCount > 0) break;
                        if (data.Position >= data.Length) break;
                        i++;
                    }
                }
            }
        }

        public struct CharacterAnimation
        {
            public float[] Values { get; set; }
        }

        public struct Header
        {
            public int HeaderSize { get; set; }
            public bool HasLayouts { get; set; }
            public int TimelineCount { get; set; }

            [JsonIgnore]
            public int UnkLizard { get; set; }

            [JsonIgnore]
            public byte UnkFlamingo { get; set; }

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
            public int FrameCountA { get; set; }

            public int FrameCountB { get; set; }

            [JsonIgnore]
            public int UnkBear { get; set; }

            public bool LoopingEnable { get; set; }
        }

        public struct Vec4
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float Z { get; set; }

            public float W { get; set; }
        }
    }

    [JsonSerializable(typeof(Lp4Test))]
    [JsonSerializable(typeof(LayoutChunk))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    public partial class Lp4TestGenerationContext : JsonSerializerContext
    {
    }
}