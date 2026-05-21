namespace FlipnicLib.Formats;

public class SaveIcon(byte [] data) : FormatBase
{
    public int IconFileId { get; init; } = GetInt32(data, 0);
    public int AnimationShapes { get; init; } = GetInt32(data, 4);
    public int TextureType { get; init; } = GetInt32(data, 8);
    public int VertexCount { get; init; } = GetInt32(data, 16);
    public Tim? Texture { get; set; }
    
    public List<Vertex> Vertices = [];

    /// <summary>
    /// Process the data provided
    /// </summary>
    public void Read()
    {
        for (var i = 0; i < VertexCount; i++)
        {
            var offset = 0x14 + (i * 24);
            Vertices.Add(new Vertex(data.Skip(offset).Take(24).ToArray()));
        }

        var animOffset = 0x14 + (VertexCount * 24);
        var frames = GetInt32(data, animOffset+16);
        var skip = 0x14;
        for (var i = 0; i < frames; i++)
        {
            var numKeys = GetInt32(data, animOffset+skip+4);
            skip += 8 + numKeys * 8;
        }
        var texOffset = animOffset + skip;
        var textureSize = GetUInt32(data, texOffset);
        Texture = new Tim(data.Skip(texOffset).Take((int)textureSize).ToArray());
    }

    public override string ToString()
    {
        var o = $"""
                 PlayStation 2 save icon

                 Magic: {IconFileId:X}
                 Shapes: {AnimationShapes}
                 Polygons: {VertexCount / 3}

                 Texture:
                 {Texture}
                 """;
        return o;
    }

    public class Vertex(byte[] vertexData)
    {
        public short CoordX { get; init; } = GetInt16(vertexData, 0);
        public short CoordY { get; init; } = GetInt16(vertexData, 2);
        public short CoordZ { get; init; } =  GetInt16(vertexData, 4);
        public ushort Light { get; init; } = GetUInt16(vertexData, 6);
        public short NormalCoordX { get; init; } = (short)-GetInt16(vertexData, 8);
        public short NormalCoordY { get; init; } =  GetInt16(vertexData, 10);
        public short NormalCoordZ { get; init; } =  (short)-GetInt16(vertexData, 12);
        public ushort NormalLight { get; init; } = GetUInt16(vertexData, 14);
        public short TextureX { get; init; } = GetInt16(vertexData, 16);
        public short TextureY { get; init; } =  GetInt16(vertexData, 18);
        public byte[] Rgba { get; init; } = [vertexData[20], vertexData[21], vertexData[22], vertexData[23]];
    }
}