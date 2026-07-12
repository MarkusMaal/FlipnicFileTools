using System.Text;

namespace FlipnicLib.Formats;

public class FormatBase
{
    
    public enum ControllerButtons : byte {
        Disabled = 0xFF,
        L2 = 0x0,
        R2,
        L1,
        R1,
        Triangle,
        Circle,
        Cross,
        Square, // no idea, but maybe 0x8 = Select?, 0xB = Start?, these won't work anyway, since they're reserved for stage status and pause menu
        L3 = 0x9,
        R3,
        DPadUp = 0xC,
        DPadRight,
        DPadDown,
        DPadLeft
    };
    
    /// <summary>
    /// Allows you to overwrite a certain part of a large array with a smaller array at a specific index
    /// </summary>
    /// <param name="outputArray">The large array, where the small array should be written to</param>
    /// <param name="offset">The address where to write the small array</param>
    /// <param name="writeableBytes">The small array</param>
    protected static void WriteByteArray(byte[] outputArray, int offset, byte[] writeableBytes)
    {
        for (var i = offset; i < offset + writeableBytes.Length; i++)
        {
            outputArray[i] = writeableBytes[i - offset];
        }
    }
    
    
    
    /// <summary>
    /// Read signed 32-bit floating point (float) from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the float requested</param>
    protected static float GetFloat(byte[] data, int offset)
    {
        try
        {
            return BitConverter.ToSingle(data.Skip(offset).Take(4).ToArray());
        }
        catch
        {
            return float.NaN;
        }
    }

    /// <summary>
    /// Decode UTF-8 string from the provided data
    /// </summary>
    /// <param name="data">Source data</param>
    /// <returns>Decoded UTF-8 string</returns>
    protected static string GetString(byte[] data)
    {
        var chars = new List<char>();
        var offset = 0;
        while (data[offset] != 0x00)
        {
            chars.Add((char)data[offset]);
            offset++;
            if ((data.Length == 0x44) && (data[offset] == 0x00) && (offset < 0x20) && (GetInt32(data, 0x40) == 0))
            {
                chars.Add(',');
                chars.Add(' ');
                offset = 0x20;
            }
            if (offset >= data.Length) break;
        }
        return new string(chars.ToArray());
    }

    
    /// <summary>
    /// Read signed 64-bit integer (long) from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static long GetInt64(byte[] data, int offset)
    {
        return BitConverter.ToInt64(data.Skip(offset).Take(8).ToArray());
    }
    
    
    /// <summary>
    /// Read signed 32-bit integer (int) from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static int GetInt32(byte[] data, int offset)
    {
        try
        {
            return BitConverter.ToInt32(data.Skip(offset).Take(4).ToArray());
        }
        catch
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Read unsigned 64-bit integer (ulong) from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static ulong GetUInt64(byte[] data, int offset)
    {
        return BitConverter.ToUInt64(data.Skip(offset).Take(8).ToArray());
    }
    
    
    /// <summary>
    /// Read unsigned 32-bit integer (uint) from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static uint GetUInt32(byte[] data, int offset)
    {
        return BitConverter.ToUInt32(data.Skip(offset).Take(4).ToArray());
    }

    /// <summary>
    /// Read signed 16-bit integer from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static short GetInt16(byte[] data, int offset)
    {
        return BitConverter.ToInt16(data.Skip(offset).Take(2).ToArray());
    }
    
    /// <summary>
    /// Read unsigned 16-bit integer from the offset specified (assuming little-endian)
    /// </summary>
    /// <param name="data">Source data</param>
    /// <param name="offset">Offset of the location within the data provided, which contains the integer requested</param>
    protected static ushort GetUInt16(byte[] data, int offset)
    {
        return BitConverter.ToUInt16(data.Skip(offset).Take(2).ToArray());
    }

    /// <summary>
    /// Convert a range withing byte array to string assuming it's UTF-8 encoded, NUL character is the delimiter
    /// </summary>
    /// <param name="data">Full range of the data</param>
    /// <param name="offset">Offset to the location within the data provided, which contains the UTF-8/ASCII string</param>
    /// <returns>Decoded UTF-8 string</returns>
    protected static string GetStringAt(byte[] data, int offset)
    {
        var chars = new List<char>();
        if (offset >= data.Length) return "";
        if (offset < 0) return "";
        while (data[offset] != 0x00)
        {
            chars.Add((char)data[offset]);
            offset++;
        }
        return new string(chars.ToArray());
    }

    /// <summary>
    /// Convert a range within byte array to string assuming it's UTF-16 encoded
    /// </summary>
    /// <param name="data">Full range of the data</param>
    /// <param name="offset">Offset to the location within the data provided, which contains the UTF-16 string</param>
    /// <param name="length">Number of bytes to read in order to decode the UTF-16 string</param>
    /// <returns>Decoded UTF-16 string</returns>
    protected static string GetFixedUtf16String(byte[] data, int offset, int length)
    {
        return Encoding.Unicode.GetString(data.Skip(offset).Take(length).ToArray());
    }

    /// <summary>
    /// Inherits StaticUtils.DotFloatString
    /// </summary>
    protected static string DotFloatString(float f)
    {
        return StaticUtils.DotFloatString(f);
    }

    /// <summary>
    /// Inherits StaticUtils.GetFilesizeString
    /// </summary>
    protected static string GetFilesizeString(long s)
    {
        return StaticUtils.GetFilesizeString(s);
    }

    /// <summary>
    /// Convert milliseconds into a time duration
    /// </summary>
    /// <param name="ms">Milliseconds to convert</param>
    /// <returns>String value in the format $"{hours:00}:{minutes:00}:{seconds:00}.{milliseconds:000}"</returns>
    protected static string GetMsAsDuration(long ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }

}