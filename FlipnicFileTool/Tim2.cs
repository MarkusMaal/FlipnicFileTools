using System.Drawing;

namespace FlipnicFileTool;

public class Tim2
{
    private byte[] bitmap;
    private byte[] pallette;

    private int Width { get; set; }
    private int Height { get; set; }

    private enum ColorMode : byte
    {
        TimMonochrome = 0x03,
        Tim4Bpp = 0x04,
        Tim8Bpp
    }
    
    private ColorMode ColorType { get; set; }
    
    public Tim2(byte[] data, bool grayscale = false)
    {
        var headerSize = BitConverter.ToInt16(data, 0x1C);
        var bitmapSize = BitConverter.ToInt32(data, 0x18);
        var paletteSize = BitConverter.ToInt32(data, 0x14);
        ColorType = (ColorMode)BitConverter.ToInt32(data, 0x23);
        this.Width = BitConverter.ToInt16(data, 0x24);
        this.Height = BitConverter.ToInt16(data, 0x26);
        this.bitmap = data.Skip(0x10+headerSize).Take(bitmapSize).ToArray();
        if (ColorType == ColorMode.Tim4Bpp)
        {
            List<byte> actualBitmap = [];
            foreach (var b in this.bitmap)
            {
                actualBitmap.Add((byte)(b % 0x10)); // second 4 bits
                actualBitmap.Add((byte)(b / 0x10)); // first 4 bits (reverse order, because little-endian)
            }
            this.bitmap = actualBitmap.ToArray();
        } else if (ColorType == ColorMode.TimMonochrome)
        {
            List<byte> shades = [];
            shades.AddRange([0,0,0,0]);
            for (var i = 1; i <= 16; i += 1)
            {
                var c = (byte)(i * 16 - 1);
                shades.AddRange([c, c, c, c]);
            }
            this.pallette = shades.ToArray();
            List<byte> actualBitmap = [];
            for (var i = 0; i < bitmap.Length; i +=4)
            {
                actualBitmap.Add((byte)(bitmap[i] / 0x10));
            }
            for (var i = 3; i < bitmap.Length; i +=4)
            {
                actualBitmap.Add((byte)(bitmap[i] % 0x10));
            }

            this.Height *= 2;
            this.bitmap = actualBitmap.ToArray();
            return;
        }
        this.pallette = data.Skip(0x10+headerSize + bitmapSize).Take(paletteSize).ToArray();
        if (this.pallette.Length == 0)
        {
            this.pallette = [255, 255, 255, 255, 0, 0, 0, 0];
        }
        if (!grayscale) return;
        List<byte> grayscalePalette = [];
        var increment = (byte)(255 / ((pallette.Length / 3) != 0 ? (pallette.Length / 3) : 1));
        if (increment == 255) increment = 1;
        byte pixel = 0x00;
        for (var i = 0; i < pallette.Length; i+=3)
        {
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(pixel);
            pixel += increment;
            if (pixel == 255)
            {
                pixel = 0;
            }
        }
        this.pallette = grayscalePalette.ToArray();
    }

    private string DisplayPalette()
    {
        string[] colHeaders = ["ID", "RGB", "Alpha"];
        List<string[]> rows = [];
        for (var i = 0; i < this.pallette.Length; i += 4)
        {
            var pal = Color.FromArgb(this.pallette[i + 3], this.pallette[i], this.pallette[i + 1],
                this.pallette[i + 2]);
            rows.Add(["0x" + (i/4).ToString(ColorType == ColorMode.Tim8Bpp ? "X2" : "X"), $"#{pal.R:X2}{pal.G:X2}{pal.B:X2}", pal.A.ToString()]);
        }
        return "Palette:\n" + StaticUtils.GenerateTable(colHeaders, rows, 9);
    }
    
    private byte[] GenerateRgbaArray()
    {
        List<Color> paletteArray = [];
        for (var i = 0; i < this.pallette.Length; i += 4)
        {
            paletteArray.Add(Color.FromArgb(this.pallette[i+3], this.pallette[i], this.pallette[i+1], this.pallette[i+2 ]));
        }
        List<byte> bitmapArray = [];
        List<byte> lineArray = [];
        int idx = 1;
        foreach (var b in bitmap.Reverse())
        {
            lineArray.Add(paletteArray[b].R);
            lineArray.Add(paletteArray[b].G);
            lineArray.Add(paletteArray[b].B);
            if (idx % Width == 0)
            {
                lineArray.Reverse();
                bitmapArray.AddRange(lineArray);
                lineArray.Clear();
            }

            idx++;
        }
        return bitmapArray.ToArray();
    }

    public void SaveBitmap(string fileName)
    {
        Console.Write("Converting...");
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
        
        imageData.AddRange(matrix);

        File.WriteAllBytes(fileName, imageData.ToArray());
        Console.WriteLine($"\rSaved as: {fileName}");
    }

    public override string ToString()
    {
        var ct = ColorType switch
        {
            ColorMode.Tim8Bpp => "8 bpp",
            ColorMode.Tim4Bpp => "4 bpp",
            _ => "Monochrome"
        };
        return $"""
                TIM2 texture file
                
                Name: {new FileInfo(Program.GetFileName()).Name}
                Width: {Width}
                Height: {Height}
                Colors: {pallette.Length}
                Palette type: {ct}
                
                {DisplayPalette()}
                """;
    }
}