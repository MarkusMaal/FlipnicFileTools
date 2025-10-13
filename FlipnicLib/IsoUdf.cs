using System.Globalization;
using FlipnicLib.Types;
using Ps2IsoTools.UDF;
using Ps2IsoTools.UDF.Files;

namespace FlipnicLib;

public class IsoUdf
{
    private readonly List<UdfFileEntry> _records = [];
    private readonly string _volumeLabel;
    
    public IsoUdf(string path)
    {
        using var reader = new UdfReader(path);
        // Get list of all files
        _volumeLabel = reader.VolumeLabel != "" ? reader.VolumeLabel : "Untitled";
        var fullNames = reader.GetAllFileFullNames();

        foreach (var name in fullNames)
        {
            var fileRead = reader.GetFileByName(name);
            if (fileRead == null) continue;
            var fileStream = reader.GetFileStream(fileRead);
            _records.Add(new UdfFileEntry
            {
                File = fileRead,
                Size = fileStream.Length,
                Path = name,
            });
        }
        
    }

    /// <summary>
    /// Extract the contents of the ISO file to a directory specified
    /// </summary>
    /// <param name="fileName">Full path to the ISO file</param>
    /// <param name="outputDir">Full path to the output directory</param>
    public void ExtractFiles(string fileName, string outputDir)
    {
        using var reader = new UdfReader(fileName);
        foreach (var f in _records)
        {
            StaticUtils.LiveLoadStatus = "Extracting " + f.Path + " (" + StaticUtils.GetFilesizeString(f.Size) + ")";
            var dir = new FileInfo(Path.Combine(outputDir, f.Path.Replace("\\", "/"))).Directory!.FullName;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            reader.CopyFile(f.File, Path.Combine(outputDir, f.Path.Replace("\\", "/")));
        }
    }

    /// <summary>
    /// Replace a file inside the .ISO file
    /// </summary>
    /// <param name="replacementFile">The file you want to integrate into the ISO file</param>
    /// <param name="isoFile">The ISO file you want to modify</param>
    /// <param name="vfsName">VFS entry inside the ISO file - this is the file you want to replace</param>
    /// <param name="rebuild">If true, let's re-build the ISO file entirely instead of just overwriting data (potentially destructive)</param>
    /// <returns>Was the replacement successful?</returns>
    public bool ReplaceFile(string replacementFile, string isoFile, string vfsName)
    {
        StaticUtils.LiveLoadStatus = $"Searching for {vfsName} in {isoFile}...";
        using var editor = new UdfEditor(isoFile);
        foreach (var f in _records.Where(f => f.Path.ToUpper() == vfsName.ToUpper().Replace("/", "\\")))
        {
            // vfsName matches, so replace this UdfFileEntry
            if (new FileInfo(replacementFile).Length > f.Size)
            {
                StaticUtils.LiveLoadStatus = "Rebuilding ISO file...";
                using var fs = File.Open(replacementFile, FileMode.Open);
                editor.ReplaceFileStream(editor.GetFileByName(f.Path) ?? throw new InvalidOperationException(), fs);
                editor.Rebuild();
                fs.Close();
                return true;
            }

            using BinaryWriter bw = new(editor.GetFileStream(f.File));
            {
                using var fs = File.OpenRead(replacementFile);
                while (fs.Position < fs.Length)
                {
                    StaticUtils.LiveLoadStatus = $"Writing new data ({Math.Round(fs.Position / (float)fs.Length * 100f, 2).ToString("F2", new CultureInfo("en-US"))}% complete)";
                    var buffer = new byte[fs.Position < fs.Length - 4096 ? 4096 : fs.Length - fs.Position];
                    fs.ReadExactly(buffer, 0, buffer.Length);
                    bw.Write(buffer);
                }
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get an array of files as VirtualFile objects
    /// </summary>
    /// <returns></returns>
    public VirtualFile[] GetFiles()
    {
        return _records.Select(rec => new VirtualFile(rec.Path, -1, rec.Size, -1, true)).ToArray();
    }
    
    public string ToString(bool asCsv)
    {
        string[] cols = ["Filename", "Size"];
        var rows = _records.Select(record => (string[])[record.Path, StaticUtils.GetFilesizeString(record.Size)]).ToList();
        return $"""
                PlayStation 2 ISO file
                File system: UDF
                Volume label: {_volumeLabel}
                
                {StaticUtils.GenerateTable(cols, rows, asCsv)}
                """;
    }
    
    public override string ToString()
    {
        return ToString(false);
    }
}

public struct UdfFileEntry
{
    public FileIdentifier File { get; init; }
    public string Path { get; init; }
    public long Size { get; init; }
}
