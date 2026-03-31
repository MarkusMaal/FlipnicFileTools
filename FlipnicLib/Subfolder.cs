using FlipnicLib.Formats;
using FlipnicLib.Types;

namespace FlipnicLib;

public class Subfolder : FormatBase
{
    private List<VirtualFile> FsEntries { get; } = [];
    private readonly Stream _ds;

    public Subfolder(Stream src)
    {
        _ds = src;
        ParseEntries();
    }

    /// <summary>
    /// Enumerates all files inside a subfolder
    /// </summary>
    private void ParseEntries()
    {
        _ds.Seek(0,  SeekOrigin.Begin);
        while (true)
        {
            var entryData = new byte[64];
            var nextEntryData = new byte[64];
            _ds.ReadExactly(entryData, 0, 64);
            _ds.ReadExactly(nextEntryData, 0, 64);
            var fileName = GetString(entryData);
            var offset = GetInt32(entryData, 60);
            if (fileName == "*End Of Mem Data")
            {
                break;
            }
            var size = GetInt32(nextEntryData, 60) - offset;
            FsEntries.Add(new VirtualFile(fileName, offset, size, _ds.Position - 128, false));
            _ds.Seek(-64, SeekOrigin.Current);
        }
        _ds.Seek(0,  SeekOrigin.Begin);
    }

    /// <summary>
    /// Overrides existing data without checking any sizes (thus unsafe)
    /// </summary>
    /// <param name="fileName">Name of the file which to replace the contents for</param>
    /// <param name="newData">Replacement data of the file</param>
    /// <param name="outputStream">A stream where to write the changes</param>
    /// <returns>Output stream</returns>
    public Stream WriteFileUnsafe(string fileName, byte[] newData, Stream outputStream)
    {
        // Find the offset
        var offset = -1;
        foreach (var entry in FsEntries.Where(entry => entry.Path == fileName))
        {
            offset = (int)entry.Offset;
        }

        // File specified is not in TOC
        if (offset == -1) return outputStream;
        
        
        // Overwrite with new data
        while (_ds.Position < _ds.Length)
        {
            if (_ds.Position == offset)
            {
                foreach (var b in newData)
                {
                    outputStream.WriteByte(b);
                    _ds.Position++;
                }
            }
            outputStream.WriteByte((byte)_ds.ReadByte());
        }
        _ds.Seek(0,  SeekOrigin.Begin);
        return outputStream;
    }
    
    /// <summary>
    /// Resizes a file and updates TOC accordingly
    /// </summary>
    /// <param name="fileName">File to resize</param>
    /// <param name="newSize">New size of the file (in bytes)</param>
    /// <param name="outputStream">Stream to write the changes to</param>
    /// <returns>Output stream</returns>
    public Stream ResizeFile(string fileName, int newSize, Stream outputStream)
    {
        var mirror = true;
        var delta = 0;
        var padOffset = 0;
        var padSize = 0;
        // Update TOC
        foreach (var entry in FsEntries)
        {
            if (entry.Path == fileName)
            {
                padOffset = (int)entry.Offset + (int)entry.Length;
            }

            var tocEntry = new byte[64];
            _ds.Seek(entry.TocOffset, SeekOrigin.Begin);
            _ds.ReadExactly(tocEntry, 0, 64);
            if (!mirror)
            {
                var entryNewSize = entry.Offset - delta;
                var newSizeBytes = BitConverter.GetBytes(entryNewSize);
                for (var i = 0; i < newSizeBytes.Length / 2; i++)
                {
                    tocEntry[60+i] = newSizeBytes[i];
                }
            }
            outputStream.Write(tocEntry, 0, 64);

            if (entry.Path != fileName) continue;
            mirror = false;
            padSize = newSize - (int)entry.Length;
            delta = (int)entry.Length - newSize;
        }
        
        // Write "*End Of Mem Data" TOC entry with new size
        _ds.Seek(60, SeekOrigin.Current);
        var currentEnd = new byte[4];
        _ds.ReadExactly(currentEnd, 0, 4);
        outputStream.Write("*End Of Mem Data"u8);
        for (var i = 0; i < 0x2C; i++)
        {
            outputStream.WriteByte(0x00);
        }

        var newEndMem = BitConverter.GetBytes(BitConverter.ToInt32(currentEnd) - delta);
        foreach (var b in newEndMem)
        {
            outputStream.WriteByte(b);
        }

        // Mirror the data except for the part where it's resized we write zeroes
        while (_ds.Position < BitConverter.ToInt32(currentEnd))
        {
            if (_ds.Position == padOffset)
            {
                for (var i = 0; i < padSize; i++)
                {
                    outputStream.WriteByte(0x00);
                }
            }
            outputStream.WriteByte((byte)_ds.ReadByte());
        }
        _ds.Seek(0,  SeekOrigin.Begin);
        return outputStream;
    }
    
    public override string ToString()
    {
        string[] colHeader = ["Name", "Offset", "Size", "TOC offset"];
        var rows = FsEntries.Select(entry => (string[])[entry.Path, "0x" + entry.OffsetX, entry.LengthX, "0x" + entry.TocOffset.ToString("X")]).ToList();
        return StaticUtils.GenerateTable(colHeader, rows, StaticUtils.SimpleOutput);
    }
}