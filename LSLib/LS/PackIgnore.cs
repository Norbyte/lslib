namespace LSLib.LS;

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class PackIgnore
{
    private readonly List<Regex> ignorePatterns = new List<Regex>();

    public PackIgnore(string directory)
    {
        LoadIgnoreList(directory);
    }

    private void LoadIgnoreList(string directory)
    {
        string ignoreFilePath = Path.Combine(directory, ".packignore");

        if (File.Exists(ignoreFilePath))
        {
            foreach (var line in File.ReadAllLines(ignoreFilePath))
            {
                string pattern = line.Trim();
                if (string.IsNullOrEmpty(pattern) || pattern.StartsWith("#"))
                    continue; // Ignore empty lines and comments

                ignorePatterns.Add(WildcardToRegex(pattern));
            }
        }
    }

    public bool IsIgnored(string relativePath)
    {

        string normalizedPath = relativePath.Replace("\\", "/");

        foreach (var pattern in ignorePatterns)
        {
            if (pattern.IsMatch(normalizedPath))
                return true;
        }

        return false;
    }

    private static Regex WildcardToRegex(string wildcard)
    {
        string pattern = Regex.Escape(wildcard)
        .Replace(@"\*", ".*")    // * = any characters
        .Replace(@"\?", ".");    // ? = any single character

        // If it ends with '/', treat it as a directory match (match everything inside)
        if (wildcard.EndsWith("/"))
        {
            pattern = pattern.TrimEnd('/') + @"/.*";
        }

        return new Regex("^" + pattern + "$", RegexOptions.IgnoreCase);
    }
}



