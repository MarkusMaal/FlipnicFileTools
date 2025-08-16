using System.Drawing;

namespace FlipnicFileTool;

public class Tim2
{
    private byte[] bitmap;
    private byte[] pallette;

    private int Width { get; set; }
    private int Height { get; set; }
    
    public Tim2(byte[] data)
    {
        var headerSize = BitConverter.ToInt16(data, 0x1C);
        var bitmapSize = BitConverter.ToInt32(data, 0x18);
        var paletteSize = BitConverter.ToInt32(data, 0x14);
        this.Width = BitConverter.ToInt16(data, 0x24);
        this.Height = BitConverter.ToInt16(data, 0x26);
        this.bitmap = data.Skip(0x10+headerSize).Take(bitmapSize).ToArray();
        this.pallette = data.Skip(0x10+headerSize + bitmapSize).Take(paletteSize).ToArray();
    }

    private byte[] GenerateRgbaArray()
    {
        List<Color> paletteArray = [];
        for (var i = 0; i < this.pallette.Length; i += 4)
        {
            paletteArray.Add(Color.FromArgb(this.pallette[i+3], this.pallette[i], this.pallette[i+1], this.pallette[i+2 ]));
        }

        List<byte> bitmapArray = [];
        foreach (var b in bitmap.Reverse())
        {
            try
            {
                bitmapArray.Add(paletteArray[b].B);
                bitmapArray.Add(paletteArray[b].G);
                bitmapArray.Add(paletteArray[b].R);
            }
            catch
            {
                bitmapArray.Add(paletteArray[0].B);
                bitmapArray.Add(paletteArray[0].G);
                bitmapArray.Add(paletteArray[0].R);
            }
        }

        return bitmapArray.ToArray();
    }

    public void SaveBitmap(string fileName)
    {
        List<byte> imageData = [0x42, 0x4D];
        var matrix = GenerateRgbaArray();
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

        // some weird stuff we need to do to mirror the image
        for (var y = 0; y < Height; y++)
        {
            for (var x = Width - 1; x >= 0; x--)
            {
                imageData.Add(matrix[y * Width * 3 + x * 3]);
                imageData.Add(matrix[y * Width * 3 + x * 3 + 1]);
                imageData.Add(matrix[y * Width * 3 + x * 3 + 2]);
            }
        }

        File.WriteAllBytes(fileName, imageData.ToArray());
    }

    public override string ToString()
    {
        return $"""
                TIM2 texture file
                
                Name: {new FileInfo(Program.GetFileName()).Name}
                Width: {Width}
                Height: {Height}
                Colors: {pallette.Length}
                """;
    }
}