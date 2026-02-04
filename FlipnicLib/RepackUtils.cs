using FlipnicLib.Types;

namespace FlipnicLib;

public abstract class RepackUtils
{
    /// <summary>
    /// Replace a file in VFS without changing the TOC. Labelled unsafe, because no checks for file size are performed
    /// </summary>
    /// <param name="offset">Offset of the file (found in TOC)</param>
    /// <param name="sourceStream">Replacement file stream</param>
    /// <param name="destinationPath">Location of the BIN file</param>
    /// <param name="padding">Total size of the file to be replaced in bytes</param>
    /// <param name="bufferSize">Size of the buffer. Must be either 1 or 2048.</param>
    public static void RepackFileUnsafe(long offset, Stream sourceStream, string destinationPath, long padding, int bufferSize = 2048)
    {
        using Stream s = File.Open(destinationPath, FileMode.Open);
        s.Position = offset;
        using Stream sr = sourceStream;
        var bytesWritten = 0;
        while (sr.Position < sr.Length)
        {
            var buffer = new byte[bufferSize];
            if (sr.Position + bufferSize > sr.Length)
            {
                buffer = new byte[(int)(sr.Length - sr.Position)];
            }
            sr.ReadExactly(buffer, 0, buffer.Length);
            s.Write(buffer, 0, buffer.Length);
            bytesWritten += buffer.Length;
        }
        
        // pad empty space if replacement file is smaller
        if (bytesWritten < padding)
        {
            while (bytesWritten < padding)
            {
                var buffer = new byte[1];
                s.Write(buffer, 0, buffer.Length);
                bytesWritten += 1;
            }
        }

        sr.Close();
        s.Close();
    }

    /// <summary>
    /// If you want to write a bigger file than the one that already exists, you need to update offsets for all later file records and increase the size of the .BIN container.
    /// Run this method BEFORE writing the bigger file.
    /// </summary>
    /// <param name="path">The file, which is going to have its size changed</param>
    /// <param name="newSize">New size of the file. The difference must be divisible by 0x800</param>
    /// <param name="binStream">The .BIN file you want to modify</param>
    /// <param name="fsEntries">File records</param>
    public static void ResizeFile(string path, int newSize, Stream binStream, VirtualFile[] fsEntries)
    {
        const int size = 2048;
        var buffer = new byte[size];
        
        StaticUtils.LiveLoadStatus = "Finding minimum offset for file resize operation";
        var originalSize = -1;
        var minOffset = -1L;
        foreach (var fe in fsEntries.Where(fe => (fe.Path[1..] == path || fe.Path == path)))
        {
            originalSize = fe.Path[1..].Contains('\\') && !fe.Path[1..].EndsWith('\\') ? (int)fe.Length : (int)(fe.Length);
            minOffset = fe.Offset;
        }
        var difference = newSize - originalSize;

        StaticUtils.LiveLoadStatus = "Updating file records with new offsets";
        using var s = binStream;
        foreach (var fe in fsEntries.Where(fe => fe.Offset > minOffset))
        {
            if (fe.TocOffset < 0) continue;
            if (fe.TocOffset >= binStream.Length) continue;
            if (!fe.LargeBuffer) continue;
            s.Seek(fe.TocOffset+0x3C, SeekOrigin.Begin);
            buffer = new byte[4];
            s.ReadExactly(buffer, 0, buffer.Length);
            var oldSize = BitConverter.ToInt32(buffer, 0);
            oldSize += difference / (fe.LargeBuffer ? 2048 : 1);
            s.Seek(fe.TocOffset + 0x3C, SeekOrigin.Begin);
            s.Write(BitConverter.GetBytes(oldSize));
        }
        
        StaticUtils.LiveLoadStatus = "Moving existing data to make room";
        buffer = new byte[size];
        var length = binStream.Length;
        binStream.SetLength(binStream.Length + difference);
        var pos = length;
        while (pos > minOffset)
        {
            var toRead = pos - size >= minOffset ? size : (int)(pos - minOffset);
            pos -= toRead;
            if (pos < 0) continue;
            binStream.Position = pos;
            binStream.ReadExactly(buffer, 0, toRead);
            binStream.Position = pos + difference;
            binStream.Write(buffer, 0, toRead);
        }

        binStream.Close();
    }
}