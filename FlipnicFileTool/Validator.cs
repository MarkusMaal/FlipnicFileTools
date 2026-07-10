using FlipnicFileTool.Help;

namespace FlipnicFileTool;

public abstract class Validator
{
    public static string ValidateArgs(string[] args, List<HelpTopic> helpTopics)
    {
        var previousArg = "";
        var appMode = "";
        var valueArgs = new Dictionary<string, string>();
        var first = false;
        foreach (var arg in args)
        {
            foreach (var _ in from helpTopic in helpTopics from line in helpTopic.GetLines() where arg.StartsWith(line.Flag.Replace("*", "").Split(' ')[0]) && line.RootFlag select line)
            {
                appMode = arg;
            }

            foreach (var line in helpTopics.SelectMany(topic => topic.GetLines()))
            {
                if (line.Flag.StartsWith(arg) && line.InputFilter.Contains('[') && arg.StartsWith("--"))
                {
                    if (first) return $"{arg} expects a value to be specified";
                }
                else if (line.Flag.StartsWith(previousArg) && line.InputFilter.Contains('[') && arg.StartsWith("--"))
                {
                    if (previousArg != "") return $"{previousArg} expects a value to be specified";
                }
            }
            first = false;

            if (previousArg.StartsWith("--") && !arg.StartsWith("--"))
            {
                if (valueArgs.ContainsKey(previousArg[2..]))
                {
                    valueArgs[previousArg[2..]] += "," + arg;
                }
                else
                {
                    valueArgs.Add(previousArg[2..], arg);
                }
            }
            previousArg = arg;
        }

        if (appMode == "")
        {
            return "Application mode not specified";
        }
        foreach (var arg in args)
        {
            if (arg == appMode) continue;
            if (!arg.StartsWith("--")) continue;
            foreach (var line in from topic in helpTopics
                     from line in topic.GetLines()
                     where line.AllowedFlags != null && !line.AllowedFlags.Contains(appMode[2..]) && string.Join(' ', args).Contains(line.Flag)
                     select line)
            {
                return $"{line.Flag} cannot be used with {appMode}";
            }
        }

        foreach (var arg in args)
        {
            foreach (var l in helpTopics.SelectMany(topic => topic.GetLines()))
            {
                if (l.Flag.Replace("*", "").StartsWith(arg))
                {
                    var filters = l.InputFilter.Split(',');
                    var dependencies = l.Dependencies;
                    foreach (var (i, dep) in dependencies.Index())
                    {
                        if (!valueArgs.TryGetValue(dep, out var depArg))
                        {
                            return $"{l.Flag} requires {dep} to be specified";
                        }

                        switch (filters[i])
                        {
                            case "*":
                                continue;
                            case "*/" when !Directory.Exists(depArg):
                                return $"Directory {depArg} does not exist";
                            case "*/":
                                continue;
                        }

                        if (Path.GetExtension(depArg) != filters[i][1..])
                        {
                            return $"When {l.Flag} is used, {dep} must be with extension {filters[i]}";
                        }
                    }
                } else if (l.Flag.Replace("*", "").Split(' ')[0] == previousArg)
                {
                    if (!l.Flag.Contains('[')) continue;
                    var filters = l.InputFilter.Split(',');
                    var flagFilters = (from filter in filters where filter.StartsWith('[') && filter.EndsWith(']') select filter.Substring(1, filter.Length - 2)).ToList();
                    foreach (var (i, subValue) in arg.Split(',').Index())
                    {
                        var currentFilter = flagFilters[i];
                        switch (currentFilter)
                        {
                            case "float":
                                if (!float.TryParse(subValue, out _))
                                    return $"Parameter {i + 1} of {l.Flag} must be float";
                                break;
                            case "uint32":
                                if (!uint.TryParse(subValue, out _))
                                    return $"Parameter {i + 1} of {l.Flag} must be an unsigned 32-bit integer";
                                break;
                            case "int32":
                                if (!int.TryParse(subValue, out _))
                                    return $"Parameter {i + 1} of {l.Flag} must be a signed 32-bit integer";
                                break;
                            case "*":
                                break;
                            default:
                                if (Path.GetExtension(subValue) != flagFilters[i][1..])
                                    return
                                        $"Parameter {i + 1} of {l.Flag} must have the extension {flagFilters[i]}";
                                break;
                        }
                    }
                }
            }

            previousArg = arg;
        }
        return "ok";
    }
}