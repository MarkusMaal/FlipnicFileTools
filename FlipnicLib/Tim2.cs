using System.Drawing;
using BigGustave;

namespace FlipnicLib;

public class Tim2
{
    private readonly byte[] _bitmap;
    private readonly byte[] _pallette;

    private int Width { get; }
    private int Height { get; }

    private enum ColorMode : byte
    {
        TimMonochrome = 0x03,
        Tim4Bpp = 0x04,
        Tim8Bpp
    }
    
    private ColorMode ColorType { get; set; }

    private string? FileName { get; set; }
    
    public Tim2(byte[] data, string fileName, bool grayscale = false)
    {
        this.FileName = fileName;
        var headerSize = BitConverter.ToInt16(data, 0x1C);
        var bitmapSize = BitConverter.ToInt32(data, 0x18);
        var paletteSize = BitConverter.ToInt32(data, 0x14);
        ColorType = (ColorMode)BitConverter.ToInt32(data, 0x23);
        this.Width = BitConverter.ToInt16(data, 0x24);
        this.Height = BitConverter.ToInt16(data, 0x26);
        this._bitmap = data.Skip(0x10+headerSize).Take(bitmapSize).ToArray();
        if (ColorType == ColorMode.Tim4Bpp)
        {
            List<byte> actualBitmap = [];
            foreach (var b in this._bitmap)
            {
                actualBitmap.Add((byte)(b % 0x10)); // second 4 bits
                actualBitmap.Add((byte)(b / 0x10)); // first 4 bits (reverse order, because little-endian)
            }
            this._bitmap = actualBitmap.ToArray();
        } else if (ColorType == ColorMode.TimMonochrome)
        {
            List<byte> shades = [];
            shades.AddRange([0,0,0,0]);
            for (var i = 1; i <= 16; i += 1)
            {
                var c = (byte)(i * 16 - 1);
                shades.AddRange([c, c, c, c]);
            }
            this._pallette = shades.ToArray();
            List<byte> actualBitmap = [];
            for (var i = 0; i < _bitmap.Length; i +=4)
            {
                actualBitmap.Add((byte)(_bitmap[i] / 0x10));
            }
            for (var i = 3; i < _bitmap.Length; i +=4)
            {
                actualBitmap.Add((byte)(_bitmap[i] % 0x10));
            }

            this.Height *= 2;
            this._bitmap = actualBitmap.ToArray();
            return;
        }
        this._pallette = data.Skip(0x10+headerSize + bitmapSize).Take(paletteSize).ToArray();
        if (this._pallette.Length == 0)
        {
            this._pallette = [255, 255, 255, 255, 0, 0, 0, 0];
        }
        if (!grayscale) return;
        List<byte> grayscalePalette = [];
        var increment = (255 / ((_pallette.Length / 3) != 0 ? (_pallette.Length / 3) : 1));
        if (increment >= 255) increment = 1;
        increment = 1;
        byte pixel = 0x00;
        var r = new Random();
        for (var i = 0; i < _pallette.Length; i+=3)
        {
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(pixel);
            grayscalePalette.Add(255);
            pixel = (byte)(pixel + increment);
            if (((increment == 1) && (pixel == 255)) || (increment == -1) && (pixel == 0))
            {
                increment = -increment;
            }
        }
        this._pallette = grayscalePalette.ToArray();
    }

    private string DisplayPalette()
    {
        string[] colHeaders = ["ID", "RGB", "Alpha"];
        List<string[]> rows = [];
        for (var i = 0; i < this._pallette.Length; i += 4)
        {
            var pal = Color.FromArgb(this._pallette[i + 3], this._pallette[i], this._pallette[i + 1],
                this._pallette[i + 2]);
            rows.Add(["0x" + (i/4).ToString(ColorType == ColorMode.Tim8Bpp ? "X2" : "X"), $"#{pal.R:X2}{pal.G:X2}{pal.B:X2}", pal.A.ToString()]);
        }
        return "Palette:\n" + StaticUtils.GenerateTable(colHeaders, rows, 9);
    }

    public byte[] GenerateRgbaArray()
    {
        List<Color> paletteArray = [];
        for (var i = 0; i < this._pallette.Length; i += 4)
        {
            paletteArray.Add(Color.FromArgb(this._pallette[i+3], this._pallette[i], this._pallette[i+1], this._pallette[i+2 ]));
        }
        List<byte> bitmapArray = [];
        List<byte> lineArray = [];
        int idx = 1;
        foreach (var b in _bitmap.Reverse())
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

    public void SavePng(Stream output)
    {
        Console.Write("Converting...");
        var builder = PngBuilder.Create(Width, Height, true);
        List<Pixel> pixels = [];
        for (var i = 0; i < _pallette.Length; i += 4)
        {
            pixels.Add(new Pixel(_pallette[i], _pallette[i+1], _pallette[i+2], _pallette[i+3], false));
        }

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                builder.SetPixel(pixels[_bitmap[y * Width + x]], x, y);
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

        output.Write(imageData.ToArray(), 0, imageData.Count);
        if (output is not FileStream fs)
        {
            Console.WriteLine($"\rLoaded image data to memory ({StaticUtils.GetFilesizeString(output.Length)})");
            return;
        }

        Console.WriteLine($"\rSaved as: {fs.Name}");
        output.Close();
    }

    public override string ToString()
    {
        var ct = ColorType switch
        {
            ColorMode.Tim8Bpp => "8 bpp",
            ColorMode.Tim4Bpp => "4 bpp",
            _ => "Monochrome"
        };
        var fn = FileName != null ? new FileInfo(FileName).Name : "???";
        return $"""
                TIM2 texture file
                
                Name: {fn}
                Width: {Width}
                Height: {Height}
                Colors: {_pallette.Length/4}
                Palette type: {ct}
                
                {DisplayPalette()}
                """;
    }
}