using BigGustave;

namespace FlipnicLib;
// specific to PlayStation 2 save icons
public class Tim
{
    private byte[] bitmap;
    private byte[] pallette;
    
    public byte[] Bitmap => bitmap;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public Tim(byte[] data)
    {
        Width = 128;
        Height = 128;
        bitmap = new byte[Width * Height * 4];
        var isCompressed = false;
        var Length = BitConverter.ToInt32(data, 0);
        if (Length == data.Length)
        {
            isCompressed = true;
        }

        byte[] decompressed = [];
        if (!isCompressed)
        {
            decompressed = data;
        }
        else
        {
            List<byte> RLEDecoded = new();
            var i = 4;
            while (i < Length)
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
                        RLEDecoded.Add(replicableData[0]);
                        RLEDecoded.Add(replicableData[1]);
                    }

                    i += 4;
                }
                else
                {
                    var blockSize = 0xFFFF - BitConverter.ToUInt16(data, i);
                    var coll = data.Skip(i + 2).Take((blockSize + 1) * 2);
                    RLEDecoded.AddRange(coll);
                    i += (blockSize * 2) + 4;
                }
            }

            decompressed = RLEDecoded.ToArray();
        }

        var bp = 0;
        const int alpha = 0xFF;
        for (var i = 0; i < Width * Height * 2; i += 2)
        {
            try
            {
                var pixelData = BitConverter.ToInt16(decompressed, i);
                var red = 8 * (pixelData & 0x1F);
                var green = 8 * ((pixelData >> 5) & 0x1F);
                var blue = 8 * (pixelData >> 10);
                bitmap[bp] = (byte)blue;
                bitmap[bp + 1] = (byte)green;
                bitmap[bp + 2] = (byte)red;
                bitmap[bp + 3] = alpha;
                bp += 4;
            }
            catch
            {
                bitmap[bp] = 0;
                bitmap[bp + 1] = 0;
                bitmap[bp + 2] = 0;
                bitmap[bp + 3] = alpha;
                bp += 4;
            }
        }
    }
    public void SavePng(Stream output)
    {
        Console.Write("Converting...");
        var builder = PngBuilder.Create(Width, Height, true);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var oneIdx = (x * 4) + y * Width * 4;
                builder.SetPixel(new Pixel(bitmap[oneIdx + 2], bitmap[oneIdx + 1], bitmap[oneIdx], bitmap[oneIdx + 3], false), x, y);
            }
        }

        builder.Save(output);
        if (output is not FileStream fs)
        {
            Console.WriteLine($"\rLoaded image data to memory ({StaticUtils.GetFilesizeString(output.Length)})");
            return;
        }
        Console.WriteLine($"\rSaved as: {fs.Name}");
        output.Close();
    }
    
    
    public void SaveBitmap(Stream output)
    {
        Console.Write("Converting...");
        List<byte> imageData = [0x42, 0x4D];
        var matrix = bitmap;
        imageData.AddRange(BitConverter.GetBytes(matrix.Length + 0x36));
        imageData.AddRange([0, 0, 0, 0]);
        imageData.AddRange([0x36, 0, 0, 0]);
        imageData.AddRange(BitConverter.GetBytes(0x28));
        imageData.AddRange(BitConverter.GetBytes(Width));
        imageData.AddRange(BitConverter.GetBytes(Height));
        imageData.AddRange([0x1, 0x00]);
        imageData.AddRange([0x18, 0x00]);
        for (var i = 0; i < 6; i++)
        {
            imageData.AddRange([0, 0, 0, 0]);   
        }
        
        imageData.AddRange(matrix);

        output.Write(imageData.ToArray(), 0, imageData.Count);
        if (output is not FileStream fs)
        {
            Console.WriteLine($"\rLoaded image data to memory ({StaticUtils.GetFilesizeString(output.Length)})");
            return;
        }

        Console.WriteLine($"\rSaved as: {fs.Name}");
        output.Close();
    }


}