namespace FlipnicLib.Formats;

public abstract class TestFormat : FormatBase
{
    public static void TestWriteByteArray(byte[] outputArray, int offset, byte[] insertArray)
    {
        WriteByteArray(outputArray, offset, insertArray);
    }

    public static float TestGetFloat(byte[] data, int offset)
    {
        return GetFloat(data, offset);
    }

    public static string TestGetString(byte[] data)
    {
        return GetString(data);
    }
    
    public static string TestGetStringAt(byte[] data, int offset)
    {
        return GetStringAt(data, offset);
    }

    public static string TestGetFilesizeString(int size)
    {
        return GetFilesizeString(size);
    }

    public static string TestDotFloatString(float value)
    {
        return DotFloatString(value);
    }
}