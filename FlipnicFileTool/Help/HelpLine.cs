namespace FlipnicFileTool.Help;

public class HelpLine(string flag, string description, string[] dependencies, string filter, bool rootFlag = false, string? allowedFlags = null)
{
    public string Flag { get; set; } = "--" + flag;
    public string Description { get; set; } = description;
    public string[] Dependencies { get; set; } = dependencies;
    public string InputFilter { get; set; } = filter;
    public bool RootFlag { get; set; } = rootFlag;
    public string? AllowedFlags { get; set; } = allowedFlags;
}