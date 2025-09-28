namespace FlipnicLib;


public class Riff
{
    /// <summary>
    /// Literally the word 'RIFF' in ASCII.
    /// </summary>
    public int ChunkID = 0x52494646;
    /// <summary>
    /// The size of this chunk.
    /// Effectively the rest of the file minus 8 bytes.
    /// </summary>
    public int ChunkSize
    {
        get { return this.data.Length + 43; }
    }
    /// <summary>
    /// Contains the letters "WAVE" in big endian.
    /// </summary>
    public int Format = 0x57415645;
    /// <summary>
    /// Contains the letters "fmt " in big endian.
    /// </summary>
    public int Subchunk1ID = 0x666d7420;
    /// <summary>
    /// The size of this chunk, which for PCM is 16.
    /// </summary>
    public int Subchunk1Size = 16;
    /// <summary>
    /// The format, PCM is 1 - other formats have different values.
    /// </summary>
    public short AudioFormat = 1;
    /// <summary>
    /// How many channels in the audio file (mono/stereo?)
    /// </summary>
    public short NumChannels = 1;
    /// <summary>
    /// How many samples per second of audio?
    /// This is the cycle rate in Hz of the sound.
    /// </summary>
    public int SampleRate = 44100;

    /// <summary>
    /// How many bytes per second of audio including all channels.
    /// </summary>
    //public int ByteRate => SampleRate * SampleRate * BitsPerSample / 8;
    public int ByteRate => 0x2B110;
    /// <summary>
    /// How many bytes per sample (mono/stereo impacts this)
    /// </summary>
    public short BlockAlign => (short)(NumChannels * BitsPerSample / 8);
    /// <summary>
    /// How many bits in each sample, 8 or 16.
    /// </summary>
    public short BitsPerSample = 16;
    /// <summary>
    /// Contains the letters 'data' in big endian.
    /// </summary>
    public int Subchunk2ID = 0x64617461;
    /// <summary>
    /// The size of data, basically how many bytes in the rest of the file.
    /// </summary>
    public int Subchunk2Size
    {
        get { return this.data.Length; }
    }
    /// <summary>
    /// The actual sound data, this is what you hear.
    /// </summary>
    public byte[] data;
    /// <summary>
    /// Returns this instances properties with endian-correctness as a byte
    /// array ready for writing to a file.
    /// </summary>
    public byte[] header => getHeaderBytes();
    /// <summary>
    /// Assembles the properties specified as a single byte array with the
    /// appropriate order and endianness.
    /// </summary>
    /// <returns></returns>
    private byte[] getHeaderBytes ()
    {
        // Create the byte array, this will store the header bytes that preced the data.
        // See the following page which reflects this breakdown.
        // http://soundfile.sapp.org/doc/WaveFormat/
        List<byte> bytes = new List<byte>();

        bool bConv = BitConverter.IsLittleEndian;

        // RIFF Chunk Descriptor
        bytes.AddRange(getBytesEndian(ChunkID, true));
        bytes.AddRange(getBytesEndian(ChunkSize, false));
        bytes.AddRange(getBytesEndian(Format, true));

        // The "fmt" (format) sub-chunk.
        bytes.AddRange(getBytesEndian(Subchunk1ID, true));
        bytes.AddRange(getBytesEndian(Subchunk1Size, false));
        bytes.AddRange(getBytesEndian(AudioFormat, false));
        bytes.AddRange(getBytesEndian(NumChannels, false));
        bytes.AddRange(getBytesEndian(SampleRate, false));
        bytes.AddRange(getBytesEndian(ByteRate, false));
        bytes.AddRange(getBytesEndian(BlockAlign, false));
        bytes.AddRange(getBytesEndian(BitsPerSample, false));

        // The "data" sub-chunk.
        bytes.AddRange(getBytesEndian(Subchunk2ID, true));
        bytes.AddRange(getBytesEndian(Subchunk2Size, false));

        return bytes.ToArray();
    }
    /// <summary>
    /// Gets the bytes for the provided value in the correct byte order.
    /// For 32-bit integers.
    /// </summary>
    /// <param name="value">The value to transform.</param>
    /// <param name="isBigEndian">Indicates whether is big-endian.</param>
    /// <returns>Correctly ordered value.</returns>
    private byte[] getBytesEndian (int value, bool isBigEndian)
    {
        // Get the bytes from the input integer.
        byte[] bytes = BitConverter.GetBytes(value);

        return setEndianness(bytes, isBigEndian);
    }
    /// <summary>
    /// Gets the bytes for the provided value in the correct byte order.
    /// For 16-bit integers.
    /// </summary>
    /// <param name="value">The value to transform.</param>
    /// <param name="isBigEndian">Indicates whether is big-endian.</param>
    /// <returns>Correctly ordered value.</returns>
    private byte[] getBytesEndian(short value, bool isBigEndian)
    {
        // Get the bytes from the input integer.
        byte[] bytes = BitConverter.GetBytes(value);

        return setEndianness(bytes, isBigEndian);
    }
    /// <summary>
    /// Reverses bytes if required to match desired byte order.
    /// </summary>
    /// <param name="isBigEndian">Indicates whether the value should be big.</param>
    /// <returns>Correctly ordered byte sequence.</returns>
    private byte[] setEndianness (byte[] bytes, bool isBigEndian)
    {
        // Work out what the default byte-order is for this system.
        bool isSystemLittle = BitConverter.IsLittleEndian;

        // If the system endian-ness doesn't match the target, reverse the bytes.
        if (isSystemLittle && isBigEndian || !isSystemLittle && !isBigEndian)
        {
            Array.Reverse(bytes);
        }

        return bytes;
    }
    /// <summary>
    /// Gets the byte data for the entire file, combining the header and payload.
    /// </summary>
    /// <returns>Bytes for the entire WAVE file.</returns>
    public byte[] GetBytes()
    {
        // Create a list to combine our header and payload.
        List<byte> bytes = new List<byte>();
        bytes.AddRange(getHeaderBytes());
        bytes.AddRange(data);
        // Return as a single array for writing to a file.
        return bytes.ToArray();
    }
    /// <summary>
    /// Instantiates a new instance of the RIFF wave file container with the
    /// provided sample rate.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz</param>
    public Riff(int sampleRate)
    {
        SampleRate = sampleRate;
    }
}