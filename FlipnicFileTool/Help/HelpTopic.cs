using FlipnicLib;
using Microsoft.VisualBasic;

namespace FlipnicFileTool.Help;

public class HelpTopic(string title, string ext, HelpLine[] lines)
{
    private string Title { get; set; } = title;
    private string Extension { get; set; } = ext;
    private HelpLine[] Lines { get; set; } = lines;

    /// <summary>
    /// Display help topic and all lines in that topic
    /// </summary>
    public void DisplayTopic()
    {
        var t = Strings.ChrW(0x23F5);
        if (OperatingSystem.IsWindows()) t = Strings.ChrW(0x25BA);
        var encoded = "~--\n";
        if (Title != "")
        {
            encoded += $"""
                        ~--{t} ~-D{Title}~-7 ({Extension})~--

                        
                        """;
        }

        encoded = Lines.Aggregate(encoded, (current, l) => current + ("~-F" + l.Flag.PadRight(36, ' ') + "~-7" + l.Description + "\n"));
        StaticUtils.DecodeColors(encoded);
    }
}