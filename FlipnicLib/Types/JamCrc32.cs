namespace FlipnicLib.Types;

public class JamCrc32
{
    private readonly uint[] _table;

    public JamCrc32()
    {
        _table = new uint[256];
        const uint polynomial = 0xedb88320;
    
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (uint j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
            _table[i] = crc;
        }
    }

    public uint ComputeChecksum(byte[] bytes)
    {
        var crc = 0xffffffff;
        foreach (var b in bytes)
        {
            var index = (byte)((crc & 0xff) ^ b);
            crc = (crc >> 8) ^ _table[index];
        }
        return (uint)(~crc ^ 0xFFFFFFFFL);
    }
}