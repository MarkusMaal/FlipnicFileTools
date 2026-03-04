using System.Drawing;
using System.Text;
using BigGustave;

namespace FlipnicLib.Formats;

// Original source: https://github.com/polymood/tim2dump/blob/main/src/
//
// Parts directly converted to C# and modified for our purposes (e.g. naming scheme
// changed to match the rest of the codebase and also the actual bitmap processing
// stuff uses different libraries)

// Copy of LICENSE for tim2dump:
/* MIT License
   
   Copyright (c) 2025 Jules P
   
   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:
   
   The above copyright notice and this permission notice shall be included in all
   copies or substantial portions of the Software.
   
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
 */
public class Tim2
{
    public string? Filename { get; set; }

    private const byte Tim2FormatVersion = 0x04; // Official TIM2 version (0x04 per spec)
    private const byte Tim2Align16 = 0x00;       // 16-byte alignment mode
    private const byte Tim2Align128 = 0x01;      // 128-byte alignment mode
    
    // Pixel format types (ImageType or ClutType low 6 bits)
    private enum PixelFormat : byte
    {
        Tim2None,
        Tim2Rgb16,
        Tim2Rgb24,
        Tim2Rgb32,
        Tim4Bpp,
        Tim8Bpp
    }

    // CLUT storage mode flags (bit 7 of ClutType)
    private enum ClutMode : byte
    {
        ClutCsm1,
        ClutCsm2 = 0x80
    }

    // File Header (16 bytes, fixed size, always at file start)
    private struct FileHeader(byte[] data)
    {
        public char[] FileId { get; } = StaticUtils.GetString(data.Take(4).ToArray()).ToCharArray();   // Must be 'T','I','M','2' to identify TIM2 file
        public byte FormatRevision { get; } = data[4];  // Format version; official spec uses 0x04
        public byte FormatId { get; } = data[5]; // Alignment: 0x00 = 16B, 0x01 = 128B
        public ushort Pictures { get; } = StaticUtils.GetUInt16(data, 6);  // Number of picture data blocks in file
        public byte[] Reserved { get; } = data.Skip(8).Take(8).ToArray();  // Must be all 0x00 (padding for 16-byte header)

        public bool IsValid => FileId.SequenceEqual("TIM2");
        public int GetAlignment => (FormatId == Tim2Align128) ? 128 : 16;
    }

    // Picture Header (48 bytes, aligned to 16 or 128 bytes)
    private struct PictureHeader(byte[] data)
    {
        public uint TotalSize { get; } = StaticUtils.GetUInt32(data, 0);  // Total bytes for this picture (headers + image + CLUT)
        public uint ClutSize { get; } = StaticUtils.GetUInt32(data, 4); // Size of CLUT data (may be 0 if no CLUT)
        public uint ImageSize { get; } = StaticUtils.GetUInt32(data, 8); // Size of image data (sum of MIPMAP levels)
        public ushort HeaderSize { get; } = StaticUtils.GetUInt16(data, 12);  // Size of headers (picture + optional MIPMAP + user space)
        public ushort ClutColors { get; } = StaticUtils.GetUInt16(data, 14);  // Actual number of colors in CLUT
        public byte PictFormat { get; } = data[0x10];  // Must be 0 for TIM2 v0x04
        public byte MipMapTextures { get; } = data[0x11];  // Number of MIPMAP textures (0x01 = LV0 only)
        public byte ClutType { get; } =  data[0x12];  // Bit7=CSM2, Bit6=compound flag, Bits0-5=pixel fmt
        public byte ImageType { get; } =  data[0x13];   // Pixel format of image data (see PixelFormat)
        public ushort ImageWidth { get; } =  StaticUtils.GetUInt16(data, 0x14);  // Width in pixels (restrictions per format/levels)
        public ushort ImageHeight { get; } =  StaticUtils.GetUInt16(data, 0x16);  // Height in pixels (no power-of-two requirement)
        public ulong GsTex0 { get; } = StaticUtils.GetUInt64(data, 0x18);  // Raw GS TEX0 register value
        public ulong GsTex1 { get; } = StaticUtils.GetUInt64(data, 0x20);  // Raw GS TEX1 register value
        public uint GsTexaFbaPabe { get; } = StaticUtils.GetUInt32(data, 0x28);  // Packed TEXA, FBA, PABE register bits
        public uint GsTexClut { get; } = StaticUtils.GetUInt32(data, 0x2C);  // TEXCLUT register (only valid if CSM2 mode)

        public bool IsClutCSM2 => (ClutType & (1 << 6)) == 0;
        public bool IsClutCompound => (ClutType & (1 << 5)) == 0;

        public PixelFormat GetImagePixelFormat() => (PixelFormat)ImageType;
        public PixelFormat GetClutPixelFormat() => (PixelFormat)(ClutType & 0x3F);
        public bool HasClut => ClutSize > 0 && ((PixelFormat)(ClutType & 0x3F) != PixelFormat.Tim2None);
        public bool HasMipMaps => MipMapTextures > 1;
    }

    // MIPMAP Header (variable size, only if mipMapTextures > 1)
    private struct MipMapHeader
    {
        public MipMapHeader(byte[] data)
        {
            GsMipTbp1 = StaticUtils.GetUInt64(data, 0);
            GsMipTbp2 = StaticUtils.GetUInt64(data, 8);
            Sizes = new uint[(data.Length - 0x10) / 4];
            for (var i = 0x10; i < data.Length; i += 0x4)
            {
                Sizes[(i - 0x10) / 4] = StaticUtils.GetUInt32(data, i);
            } 
        }

        public ulong GsMipTbp1 { get; }       // GS MIPTBP1 register (LV1–LV3 buffer base/width)
        public ulong GsMipTbp2 { get; }       // GS MIPTBP2 register (LV4–LV6 buffer base/width)
        public uint[] Sizes { get; }          // Size (bytes) for each MIPMAP level (aligned to 16B)
    }

    // Extended Header (optional, 16 bytes, starts user space)
    private struct ExtendedHeader(byte[] data)
    {
        public byte[] ExHeaderId { get; } = data.Take(4).ToArray();       // 'e','X','t','\0' identifier
        public uint UserSpaceSize = StaticUtils.GetUInt32(data, 4); // Valid total size of user space (incl. this header)
        public uint UserDataSize = StaticUtils.GetUInt32(data, 8);  // Bytes of user data before comment string
        public uint Reserved = StaticUtils.GetUInt32(data, 12);     // Must be 0x00000000
        public bool IsValid => ExHeaderId.SequenceEqual("eXt\0"u8.ToArray());
    }
    
    // GS Register field extraction helpers (TEX0/TEX1 parsing)
    private struct GsTex0Fields(byte[] data)
    {
        public ushort Tbp0 { get; set; } = StaticUtils.GetUInt16(data, 0);   // Texture base pointer
        public byte Tbw { get; set; } = data[2];    // Texture buffer width
        public byte Psm { get; set; } = data[3];    // Pixel storage mode
        public byte Tw { get; set; } = data[4];     // Texture width log2
        public byte Th { get; set; } = data[5];     // Texture height log2
        public byte Tcc { get; set; } = data[6];    // Texture color component
        public byte Tfx { get; set; } = data[7];    // Texture function
        public ushort Cbp { get; set; } = StaticUtils.GetUInt16(data, 8);    // CLUT buffer base
        public byte Cpsm { get; set; } = data[10];   // CLUT pixel storage mode
        public byte Csm { get; set; } = data[11];    // CLUT storage mode (0=CSM1, 1=CSM2)
        public byte Csa { get; set; } = data[12];    // CLUT entry offset
        public byte Cld { get; set; } = data[13];    // CLUT buffer load control

        public static GsTex0Fields Parse(ulong tex0) {
            var f = new GsTex0Fields
            {
                Tbp0 = (ushort)(tex0 & 0x3FFF),
                Tbw = (byte)((tex0 >> 14) & 0x3F),
                Psm = (byte)((tex0 >> 20) & 0x3F),
                Tw = (byte)((tex0 >> 26) & 0x0F),
                Th = (byte)((tex0 >> 30) & 0x0F),
                Tcc = (byte)((tex0 >> 34) & 0x01),
                Tfx = (byte)((tex0 >> 35) & 0x03),
                Cbp = (ushort)((tex0 >> 37) & 0x3FFF),
                Cpsm = (byte)((tex0 >> 51) & 0x0F),
                Csm = (byte)((tex0 >> 55) & 0x01),
                Csa = (byte)((tex0 >> 56) & 0x1F),
                Cld = (byte)((tex0 >> 61) & 0x07)
            };
            return f;
        }
    };
    
    
    private struct GsTex1Fields(byte[] data) {
        public byte  Lcm { get; set; } = data[0];    // LOD calculation method
        public byte  Mxl { get; set; } = data[1];    // Maximum MIP level
        public byte  Mmag { get; set; } = data[2];   // Filter when texture expanded
        public byte  Mmin { get; set; } = data[3];   // Filter when texture reduced
        public byte  Mtba { get; set; } = data[4];   // MIPMAP base address spec
        public ushort L { get; set; } = StaticUtils.GetUInt16(data, 5);      // LOD parameter L
        public ushort K { get; set; } = StaticUtils.GetUInt16(data, 7);      // LOD parameter K

        public static GsTex1Fields Parse(ulong tex1) {
            GsTex1Fields f = new()
            {
                Lcm = (byte)(tex1 & 0x01),
                Mxl = (byte)((tex1 >> 2) & 0x07),
                Mmag = (byte)((tex1 >> 5) & 0x01),
                Mmin = (byte)((tex1 >> 6) & 0x07),
                Mtba = (byte)((tex1 >> 9) & 0x01),
                L = (ushort)((tex1 >> 19) & 0x03),
                K = (ushort)((tex1 >> 32) & 0xFFF)
            };
            return f;
        }
    };

    private struct Color32(byte r, byte g, byte b, byte a)
    {
        public byte R = r, G = g, B = b, A = a;

        public Color ToStdColor()
        {
            return Color.FromArgb(A, R, G, B);
        }

        public Color32() : this(0, 0, 0, 255) {}
    }

    private struct Color16(ushort value)
    {
        public readonly ushort Value = value;

        public Color32 ToColor32()
        {
            var r = (byte)(((Value & 0x001F) << 3) | ((Value & 0x001F) >> 2));
            var g = (byte)(((Value & 0x03E0) >> 2) | ((Value & 0x03E0) >> 7));
            var b = (byte)(((Value & 0x7C00) >> 7) | ((Value & 0x7C00) >> 12));
            var a = (Value & 0x8000) != 0 ? (byte)255 : (byte)0;
            return new Color32(r, g, b, a);
        }
    }

    private string PixelFormatToString(PixelFormat fmt)
    {
        return fmt switch
        {
            PixelFormat.Tim2None => "None",
            PixelFormat.Tim2Rgb16 => "RGB16",
            PixelFormat.Tim2Rgb24 => "RGB24",
            PixelFormat.Tim2Rgb32 => "RGB32",
            PixelFormat.Tim4Bpp => "IDTEX4 (4-bit indexed)",
            PixelFormat.Tim8Bpp => "IDTEX8 (8-bit indexed)",
            _ => "Unknown"
        };
    }

    private int GetBitsPerPixel(PixelFormat fmt)
    {
        return fmt switch
        {
            PixelFormat.Tim4Bpp => 4,
            PixelFormat.Tim8Bpp => 8,
            PixelFormat.Tim2Rgb16 => 16,
            PixelFormat.Tim2Rgb24 => 24,
            PixelFormat.Tim2Rgb32 => 32,
            _ => 0
        };
    }


    // ─────────────────────────────────────────────────────────────
    // Picture implementation
    // ─────────────────────────────────────────────────────────────
    class Picture
    {
        public PictureHeader Header { get; set; }
        public MipMapHeader? MipMapHeader { get; set; }
        public byte[] UserData { get; set; }
        public byte[] ImageData { get; set; }
        public byte[]? ClutData { get; set; }
        public ExtendedHeader? ExtHeader { get; set; }
        public string Comment { get; set; }
        public bool ClutCompoundFailed = false;

        /**
         * Turn one TIM2 picture into a flat RGBA8 buffer.
         *
         * - mipLevel 0 is the largest (top) level. If the picture has no mipmaps,
         *   mipLevel must be 0.
         * - We decode according to header.imageType. For indexed formats we fetch the CLUT,
         *   apply the right ordering rules (CSM1/compound), and output Color32.
         * - On invalid input (e.g., mipLevel out of range), we return an empty vector.
         *
         * NOTE: This method reads pixels one by one via getPixelColor(). That’s simple,
         * but not the fastest. If you need speed, consider a scanline decoder.
         */
        public Color32[] DecodeImage(int mipLevel = 0)
        {
            if (mipLevel >= Header.MipMapTextures)
            {
                return [];
            }
            var width = GetMipMapWidth(mipLevel);
            var height = GetMipMapHeight(mipLevel);
            
            var result = new Color32[width * height];

            var done = 0;
            // multithreading here helps speed up the conversion up to 4x
            new Thread(() =>
            {

                for (var y = 0; y < height; ++y)
                {
                    if (y % 2 == 0) continue;
                    for (var x = 0; x < width; ++x)
                    {
                        if (x % 2 == 0) continue;
                        result[y * width + x] = GetPixelColor(x, y, mipLevel);
                    }
                }

                done++;
            }).Start();

            new Thread(() =>
            {

                for (var y = 0; y < height; ++y)
                {
                    if (y % 2 == 1) continue;
                    for (var x = 0; x < width; ++x)
                    {
                        if (x % 2 == 1) continue;
                        result[y * width + x] = GetPixelColor(x, y, mipLevel);
                    }
                }

                done++;
            }).Start();
            new Thread(() =>
            {

                for (var y = 0; y < height; ++y)
                {
                    if (y % 2 == 0) continue;
                    for (var x = 0; x < width; ++x)
                    {
                        if (x % 2 == 1) continue;
                        result[y * width + x] = GetPixelColor(x, y, mipLevel);
                    }
                }

                done++;
            }).Start();
            new Thread(() =>
            {

                for (var y = 0; y < height; ++y)
                {
                    if (y % 2 == 1) continue;
                    for (var x = 0; x < width; ++x)
                    {
                        if (x % 2 == 0) continue;
                        result[y * width + x] = GetPixelColor(x, y, mipLevel);
                    }
                }

                done++;
            }).Start();
            
            while (done != 4)
            {
                Thread.Sleep(1);
            }
            return result;
        }

        /**
         * Decode the CLUT (palette) into RGBA colors.
         *
         * - Returns empty if there is no CLUT.
         * - Handles CSM1 “compound” index reordering (TIM2 spec §4.5).
         * - Converts RGB16/24/32 entries to Color32.
         *
         * IMPORTANT (spec detail):
         *   For CSM1 + RGB16/24/32, the spec describes additional intra-block byte swaps
         *   (every 64/96/128 bytes). This implementation currently reorders indices for
         *   compound mode but does NOT perform those byte-block swaps. If you encounter
         *   incorrect colors for certain CSM1 CLUTs, implement that swap step here.
         *   (See TIM2 spec §4.5 for the exact byte swap rules.)
         */
        public Color32[] GetClutColors(bool forceNoCompound = false)
        {
            try
            {
                Color32[] colors = [];
                if (!Header.HasClut) return colors;
                colors = new Color32[Header.ClutColors];

                var data = ClutData;

                var fmt = Header.GetClutPixelFormat();
                var isCompound = Header.IsClutCompound;

                for (var i = 0; i < Header.ClutColors; ++i)
                {
                    var index = i;

                    // CSM1 “compound” mode shuffles palette indices inside 32-color blocks.
                    // This implements the index remap table described in §4.5 (the 0..31 matrix).
                    if (!forceNoCompound) // this hacky solution will be here until I figure out how to properly detect if CLUT has compound or not 
                    {
                        var block = i / 32;
                        var localIdx = i % 32;

                        // This is the minimal reordering that matches the 32-entry table.
                        // (It swaps [8..15] with [16..23] by +8 / -8.)
                        if (localIdx >= 8 && localIdx < 16)
                        {
                            localIdx += 8;
                        }
                        else if (localIdx >= 16 && localIdx < 24)
                        {
                            localIdx -= 8;
                        }

                        index = block * 32 + localIdx;
                    }

                    Color32 color = new();

                    int byteIdx;
                    switch (fmt)
                    {
                        case PixelFormat.Tim2Rgb16:
                            byteIdx = index * 2;
                            var val = StaticUtils.GetUInt16(data, byteIdx);
                            Color16 c16 = new(val);
                            color = c16.ToColor32();
                            break;
                        case PixelFormat.Tim2Rgb24:
                            byteIdx = index * 3;
                            color.R = data[byteIdx + 0];
                            color.G = data[byteIdx + 1];
                            color.B = data[byteIdx + 2];
                            color.A = 255;
                            break;
                        case PixelFormat.Tim2Rgb32:
                            byteIdx = index * 4;
                            color.R = data[byteIdx + 0];
                            color.G = data[byteIdx + 1];
                            color.B = data[byteIdx + 2];
                            color.A = data[byteIdx + 3];
                            break;
                        default:
                            break;
                    }

                    colors[i] = color;
                }

                return colors;
            }
            catch (IndexOutOfRangeException)
            {
                ClutCompoundFailed = true;
                return GetClutColors(true);
            }
        }
        /**
         * Return the color of a single pixel at (x, y) for a given mip level.
         *
         * - True-color formats read directly from imageData.
         * - Indexed formats first read the index, then look up into the decoded CLUT.
         *
         * NOTE on RGB24:
         *   The GS stores RGB24 in a packed layout. The code below treats pixels as tightly
         *   packed 3-byte triplets laid sequentially. If you run into RGB24 TIM2s that look
         *   scrambled, you likely need to implement the exact GS swizzle/packing for 24-bit
         *   textures as described in the spec (§4.6). Consider that a future optimization.
         */
        private Color32 GetPixelColor(int x, int y, int mipLevel = 0)
        {
            var offset = GetImageOffset(mipLevel);
            var width = GetMipMapWidth(mipLevel);
            var data = ImageData;
            var dataStart = offset;

            Color32 result = new();

            int idx;
            switch (Header.GetImagePixelFormat())
            {
                case PixelFormat.Tim2Rgb32:
                    idx = (y * width + x) * 4 + dataStart;
                    result.R = data[idx + 0];
                    result.G = data[idx + 1];
                    result.B = data[idx + 2];
                    result.A = data[idx + 3];
                    break;
                case PixelFormat.Tim2Rgb24:
                    // Packed 3 bytes per pixel, no padding between pixels here.
                    // See note above if you encounter layout mismatches.
                    idx = (y * width + x) * 3 + dataStart;
                    result.R = data[idx + 0];
                    result.G = data[idx + 1];
                    result.B = data[idx + 2];
                    result.A = 255;
                    break;
                case PixelFormat.Tim2Rgb16:
                    idx = (y * width + x) * 2 + dataStart;
                    var val = BitConverter.ToUInt16(data, idx);
                    Color16 c16 = new(val);
                    result = c16.ToColor32();
                    break;
                case PixelFormat.Tim8Bpp:
                    if (Header.HasClut)
                    {
                        idx = y * width + x + dataStart;
                        var colorIdx = data[idx];
                        var colors = GetClutColors();
                        if (colorIdx < colors.Length)
                        {
                            result = colors[colorIdx];
                        }
                    }
                    break;
                case PixelFormat.Tim4Bpp:
                    if (Header.HasClut)
                    {
                        var pixelIdx = y * width + x;
                        var byteIdx = pixelIdx / 2 + dataStart;
                        // Even pixel = low nibble, odd pixel = high nibble.
                        var packed = data[byteIdx];
                        var colorIdx = (byte)((pixelIdx & 1) != 0 ? (packed >> 4) : (packed & 0x0F));
                        var colors = GetClutColors();
                        if (colorIdx < colors.Length)
                        {
                            result = colors[colorIdx];
                        }
                    }
                    break;
            }

            return result;
        }
        /**
         * Compute the byte offset to the start of a given mip level within imageData.
         *
         * - If there’s no MIPMAP header (i.e., mipMapTextures == 1), level 0 always starts at 0.
         * - If present, mipMapHeader->sizes holds each level’s byte length (already 16-byte aligned).
         */
        private int GetImageOffset(int mipLevel)
        {
            if ((MipMapHeader == null) || mipLevel == 0) return 0;
            var offset = 0;
            for (var i = 0; i < mipLevel && i < MipMapHeader?.Sizes.Length; ++i)
            {
                offset += (int)MipMapHeader?.Sizes[i]!;
            }

            return offset;
        }
        
        /**
         * Width of a given mip level. We shift-right per level and clamp to at least 1.
         */
        private int GetMipMapWidth(int level)
        {
            return int.Max(1, Header.ImageWidth >> level);
        }
        /**
         * Height of a given mip level. We shift-right per level and clamp to at least 1.
         */
        private int GetMipMapHeight(int level)
        {
            return int.Max(1, Header.ImageHeight >> level);
        }
    }

    private bool ParseFileHeader(Stream file)
    {
        var headerData = new byte[0x10];
        file.ReadExactly(headerData, 0, 0x10);
        m_fileHeader = new FileHeader(headerData);
        return m_fileHeader.IsValid;
    }

    private FileHeader m_fileHeader;
    private Picture[] m_pictures = [];

    public Tim2(byte[] data)
    {
        var ms = new MemoryStream(data);
        if (!ParseFileHeader(ms))
        {
            throw new FormatException("Invalid TIM2 file signature");
        }
        var alignment = m_fileHeader.GetAlignment;
        SkipAlignment(ms, alignment);
        m_pictures = new Picture[m_fileHeader.Pictures];
        
        for (ushort i = 0; i < m_fileHeader.Pictures; ++i) {
            Picture pic = new();
            if (!ParsePicture(ms, pic, alignment))
            {
                throw new Exception($"Failed to parse picture {i}");
            }
            m_pictures[i] = pic;
        }
    }

    private bool ParsePicture(Stream file, Picture pic, int alignment)
    {
        var headerData = new byte[48];
        file.ReadExactly(headerData, 0, 48);
        pic.Header = new PictureHeader(headerData);
        
        // MIP map header (only if more than one level)
        if (pic.Header.MipMapTextures > 1) {
            if (!ParseMipMapHeader(file, pic)) {
                return false;
            }
        }
        
        // If headerSize is bigger than what we already consumed, the remainder is “user space”.
        var headerDataSize = file.Position;
        if (pic.MipMapHeader != null)
        {
            var mipHeaderSize = 16 + pic.Header.MipMapTextures * 4; // 16 = two u64 fields
            mipHeaderSize = AlignOffset(mipHeaderSize, 16);
            headerDataSize += mipHeaderSize;
        }

        if (pic.Header.HeaderSize > headerDataSize) {
            if (!ParseUserSpace(file, pic)) {
                return false;
            }
        }
        
        var currentPos = (int)(file.Position);
        var imageStart = AlignOffset(currentPos, alignment);
        if (imageStart > currentPos) file.Seek(imageStart, SeekOrigin.Begin);
        
        // Image data (may be 0 for CLUT-only pictures)
        if (pic.Header.ImageSize > 0)
        {
            ParseImageData(file, pic);
        }

        currentPos = (int)(file.Position);
        var clutStart  = AlignOffset(currentPos, alignment);
        if (clutStart > currentPos) file.Seek(clutStart,  SeekOrigin.Begin);
        
        // CLUT data (only for indexed formats; size may still be 0)
        if (pic.Header.ClutSize > 0)
        {
            ParseClutData(file, pic);
        }
        return true;
    }

    private bool ParseMipMapHeader(Stream file, Picture pic)
    {
        throw new NotImplementedException();
    }

    private bool ParseUserSpace(Stream file, Picture pic)
    {
        throw new NotImplementedException();
    }
    
    /**
     * Read raw image bytes (GS layout, not decoded).
     */
    private void ParseImageData(Stream file, Picture pic)
    {
        pic.ImageData = new byte[pic.Header.ImageSize];
        file.ReadExactly(pic.ImageData, 0, (int)pic.Header.ImageSize);
    }
    
    /**
     * Read raw CLUT bytes (not decoded).
     */
    private void ParseClutData(Stream file, Picture pic)
    {
        pic.ClutData = new byte[pic.Header.ClutSize];
        file.ReadExactly(pic.ClutData, 0, (int)pic.Header.ClutSize);
    }

    /**
     * Round “offset” up to the next multiple of “alignment”.
     * e.g., alignOffset(17, 16) == 32.
     */
    private int AlignOffset(int offset, int alignment) {
        return ((offset + alignment - 1) / alignment) * alignment;
    }
    
    private void SkipAlignment(Stream file, int alignment)
    {
        var currentPos = (int)file.Position;
        var alignedPos = AlignOffset(currentPos, alignment);
        if (alignedPos > currentPos)
        {
            file.Seek(alignedPos, SeekOrigin.Begin);
        }
    }

    public Tim2(byte[] data, string filename) : this(data)
    {
        Filename = filename;
    }
    
    private string DisplayPalette(bool asCsv)
    {
        string[] colHeaders = ["ID", "RGB", "Alpha"];
        List<string[]> rows = [];
        if (this.m_pictures[0].ClutData == null)
        {
            return "This texture does not contain an indexed palette";
        }
        for (var i = 0; i < this.m_pictures[0].ClutData.Length; i += 4)
        {
            var pal = Color.FromArgb(this.m_pictures[0].ClutData[i + 3], this.m_pictures[0].ClutData[i],
                this.m_pictures[0].ClutData[i + 1],
                this.m_pictures[0].ClutData[i + 2]);
            rows.Add(["0x" + (i / 4).ToString("X"), $"#{pal.R:X2}{pal.G:X2}{pal.B:X2}", pal.A.ToString()]);
        }
        return "Palette:\n" + StaticUtils.GenerateTable(colHeaders, rows, asCsv);
    }

    /// <summary>
    /// Convert the TIM2 file to PNG
    /// </summary>
    /// <param name="output">Output .PNG file stream</param>
    public void SavePng(Stream output)
    {
        var complete = false;
        new Thread(() =>
        {
            StaticUtils.LoadIdx = 1;
            while (!complete)
            {
                StaticUtils.PrintLoader();
                Console.Write("  Converting...    \r");
                Thread.Sleep(100);
                StaticUtils.LoadIdx+=1000;
            }
            Console.Write("                    \r");
        }).Start();
        var builder = PngBuilder.Create(m_pictures[0].Header.ImageWidth, m_pictures[0].Header.ImageHeight, true);
        var i = 0;
        StaticUtils.LiveLoadStatus = "Converting image(s)";
        foreach (var pixel in m_pictures[0].DecodeImage(m_pictures[0].Header.MipMapTextures - 1))
        {
            var y = i / m_pictures[0].Header.ImageWidth;
            var x = i % m_pictures[0].Header.ImageWidth;
            builder.SetPixel(new Pixel(pixel.R, pixel.G, pixel.B, pixel.A, false), x, y);


            i++;
        }

        complete = true;
        builder.Save(output);
        if (output is not FileStream fs)
        {
            StaticUtils.DecodeColors($"~-B\rInfo~--: Loaded image data to memory ({StaticUtils.GetFilesizeString(output.Length)})\n");
            return;
        }
        Console.WriteLine($"\rSaved as: {fs.Name}");
        output.Close();
    }
    
    public string ToString(bool asCsv)
    {
        var ct = PixelFormatToString((PixelFormat)m_pictures[0].Header.ImageType);
        var fn = Filename != null ? new FileInfo(Filename).Name : "???";
        var cC = (uint)(m_pictures[0].ClutData != null ? m_pictures[0].ClutData!.Length / 4 : 0);
        var cmpC = (!m_pictures[0].ClutCompoundFailed) ? "Compound" : "Non-compound";
        var pT = $"Palette type: {ct} ({cmpC})";
        pT = pT.Replace(") (", "/");
        if (cC == 0)
        {
            cC = (PixelFormat)m_pictures[0].Header.ImageType switch
            {
                PixelFormat.Tim2Rgb24 => (uint)Math.Pow(2, 24),
                PixelFormat.Tim2Rgb32 => (uint)Math.Pow(2, 32),
                PixelFormat.Tim2Rgb16 => (uint)Math.Pow(2, 16),
                _ => cC
            };
        }
        return $"""
                TIM2 texture file

                Name: {fn}
                Width: {m_pictures[0].Header.ImageWidth}
                Height: {m_pictures[0].Header.ImageHeight}
                Colors: {cC}
                {pT}
                Pictures: {m_pictures.Length}

                {DisplayPalette(asCsv)}
                """;
    }
    
    public override string ToString()
    {
        return ToString(false);
    }

    public void ReplaceColor(byte[] rgb)
    {
        foreach (var img in m_pictures)
        {
            if (img.ClutData == null) continue;
            var newClutData = new byte[img.ClutData.Length];
            for (var i = 0; i <  img.ClutData.Length; i+=4)
            {
                newClutData[i] = StaticUtils.ForceNoColors ? (byte)0x00 : rgb[0];
                newClutData[i+1] = StaticUtils.ForceNoColors ? (byte)0x00 :rgb[1];
                newClutData[i+2] = StaticUtils.ForceNoColors ? (byte)0x00 :rgb[2];
                newClutData[i+3] = img.ClutData[i+3];
            }
            img.ClutData = newClutData;
        }
    }
}