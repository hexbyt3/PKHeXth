using System;

namespace PKHeX.Core;

public static class UpdateUtil
{
    /// <summary>
    /// Gets the latest version of PKHeX according to the GitHub API
    /// </summary>
    /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined</returns>
    public static Version? GetLatestPKHeXVersion()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeXth/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Parse it manually; no need to parse the entire json to object.
        const string tag = "tag_name";
        var index = responseJson.IndexOf(tag, StringComparison.Ordinal);
        if (index == -1)
            return null;

        var first = responseJson.IndexOf('"', index + tag.Length + 1) + 1;
        if (first == 0)
            return null;
        var second = responseJson.IndexOf('"', first);
        if (second == -1)
            return null;

        var tagString = responseJson[first..second];

        // Normalize fork tag formats so System.Version can parse them.
        // Strip a leading "v" if present (e.g. "v26.05.05") and convert
        // the "-rev.N" revision suffix into a fourth version component
        // (e.g. "26.05.05-rev.3" -> "26.05.05.3").
        if (tagString.Length > 0 && (tagString[0] == 'v' || tagString[0] == 'V'))
            tagString = tagString[1..];
        tagString = tagString.Replace("-rev.", ".").Replace("-rev", ".");

        return !Version.TryParse(tagString, out var latestVersion) ? null : latestVersion;
    }
}
