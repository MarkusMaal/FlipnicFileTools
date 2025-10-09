using System.IO;

namespace FlipnicFileToolGUI.Helpers;

public abstract class RepackUtils
{
    /// <summary>
    /// Replace a file in VFS without changing the TOC. Labelled unsafe, because no checks for file size are performed
    /// </summary>
    /// <param name="offset">Offset of the file (found in TOC)</param>
    /// <param name="sourcePath">Replacement file path</param>
    /// <param name="destinationPath">Location of the BIN file</param>
    /// <param name="padding">Total size of the file to be replaced in bytes</param>
    /// <param name="bufferSize">Size of the buffer. Must be either 1 or 2048.</param>
    public static void RepackFileUnsafe(int offset, string sourcePath, string destinationPath, long padding, int bufferSize = 2048)
    {
        using Stream s = File.Open(destinationPath, FileMode.Open);
        s.Position = offset * (long)bufferSize;
        using Stream sr = File.OpenRead(sourcePath);
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
}