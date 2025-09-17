namespace FlipnicFileTool.Help;

public class HelpLine(string flag, string description)
{
    public string Flag { get; set; } = "--" + flag;
    public string Description { get; set; } = description;
}