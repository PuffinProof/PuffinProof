namespace JustSpell.Core;

public static class AppPaths
{
    public static string AppDataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JustSpell");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string SettingsFile => Path.Combine(AppDataDirectory, "settings.json");

    public static string UserWordsFile => Path.Combine(AppDataDirectory, "user-words.txt");

    public static string BundledDictionariesDirectory
    {
        get
        {
            foreach (var dir in CandidateDictionaryDirectories())
            {
                if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "en_US.dic")))
                {
                    return dir;
                }
            }

            return Path.Combine(AppContext.BaseDirectory, "Dictionaries");
        }
    }

    private static IEnumerable<string> CandidateDictionaryDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Dictionaries");

        var processDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrWhiteSpace(processDir))
        {
            yield return Path.Combine(processDir, "Dictionaries");
        }

        // Tests and `dotnet run` from the repo.
        yield return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "JustSpell", "Dictionaries"));
    }
}
