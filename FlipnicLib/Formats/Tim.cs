using BigGustave;

namespace FlipnicLib.Formats;
// specific to PlayStation 2 save icons
public class Tim : FormatBase
{
    private readonly byte[] _bitmap;

    private int Width { get; set; }
    private int Height { get; set; }

    private int CompressedSize { get; set; }
    private int DecompressedSize { get; set; }

    public Tim(byte[] data)
    {
        Width = 128;
        Height = 128;
        CompressedSize = data.Length;
        _bitmap = new byte[Width * Height * 4];
        var isCompressed = false;
        var length = BitConverter.ToInt32(data, 0);
        if (length == data.Length)
        {
            isCompressed = true;
        }

        byte[] decompressed;
        if (!isCompressed)
        {
            decompressed = data;
        }
        else
        {
            List<byte> rleDecoded = new();
            var i = 4;
            while (i < length)
            {
                var code = BitConverter.ToUInt16(data, i);
                if (code < 0xFF00)
                {
                    byte[] replicableData = [data[i + 2], data[i + 3]];
                    if (i % 2 != 0)
                    {
                        i += 1;
                        continue;
                    }

                    for (var c = 0; c < code; c++)
                    {
                        rleDecoded.Add(replicableData[0]);
                        rleDecoded.Add(replicableData[1]);
                    }

                    i += 4;
                }
                else
                {
                    var blockSize = 0xFFFF - BitConverter.ToUInt16(data, i);
                    var coll = data.Skip(i + 2).Take((blockSize + 1) * 2);
                    rleDecoded.AddRange(coll);
                    i += (blockSize * 2) + 4;
                }
            }

            decompressed = rleDecoded.ToArray();
        }
        DecompressedSize = decompressed.Length;

        var bp = 0;
        const int alpha = 0xFF;
        var pixelData = BitConverter.ToInt16(decompressed, 0);
        int[] fallBack = [8 * (pixelData & 0x1F), 8 * ((pixelData >> 5) & 0x1F), 8 * (pixelData >> 10)];
        for (var i = 0; i < Width * Height * 2; i += 2)
        {
            try
            {
                pixelData = BitConverter.ToInt16(decompressed, i);
                var red = 8 * (pixelData & 0x1F);
                var green = 8 * ((pixelData >> 5) & 0x1F);
                var blue = 8 * (pixelData >> 10);
                _bitmap[bp] = (byte)blue;
                _bitmap[bp + 1] = (byte)green;
                _bitmap[bp + 2] = (byte)red;
                _bitmap[bp + 3] = alpha;
                bp += 4;
            }
            catch
            {
                _bitmap[bp] = (byte)fallBack[0];
                _bitmap[bp + 1] = (byte)fallBack[1];
                _bitmap[bp + 2] = (byte)fallBack[2];
                _bitmap[bp + 3] = alpha;
                bp += 4;
            }
        }
    }
    
    /// <summary>
    /// Convert the TIM file to PNG
    /// </summary>
    /// <param name="output">Output .PNG file stream</param>
    public void SavePng(Stream output)
    {
        Console.Write("Converting...");
        var builder = PngBuilder.Create(Width, Height, true);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var oneIdx = (x * 4) + y * Width * 4;
                builder.SetPixel(new Pixel(_bitmap[oneIdx + 2], _bitmap[oneIdx + 1], _bitmap[oneIdx], _bitmap[oneIdx + 3], false), x, y);
            }
        }

        builder.Save(output);
        if (output is not FileStream fs)
        {
            StaticUtils.DecodeColors($"~-B\rInfo~--: Loaded image data to memory ({GetFilesizeString(output.Length)})\n");
            return;
        }
        Console.WriteLine($"\rSaved as: {fs.Name}");
        output.Close();
    }

    public override string ToString()
    {
        var typeS = ((DecompressedSize != CompressedSize) ? "RLE compressed" : "Uncompressed");
        return $"""
                
                Width: {Width}
                Height: {Height}
                
                Type: {typeS}
                Compressed size: {GetFilesizeString(CompressedSize)}
                Decompressed size: {GetFilesizeString(DecompressedSize)}
                """;
    }


}