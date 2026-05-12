using System.Text.RegularExpressions;

namespace WindowsExplorerContextTools.Services;

public class PathChecker
{
    private static readonly Regex FileRegex = new Regex(@"\.[^\\\/]+$|^[^\\\/]+\.[^\\\/]+_\d+_\d+\.\d+\.\d+\.\d+_\d+\.\d+\.\d+$");

    public static bool IsFile(string path)
    {
        return FileRegex.IsMatch(path);
    }

    public static bool IsFolder(string path)
    {
        return !FileRegex.IsMatch(path);
    }
}
