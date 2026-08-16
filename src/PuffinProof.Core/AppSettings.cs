namespace PuffinProof.Core;

public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public bool StartWithWindows { get; set; }

    public string Language { get; set; } = "en_US";

    public int PopupDurationSeconds { get; set; } = 6;

    public bool IgnoreWordsWithDigits { get; set; } = true;

    public int MinWordLength { get; set; } = 2;

    public List<string> ExcludedProcesses { get; set; } =
    [
        "keepass",
        "keepassxc",
        "bitwarden",
        "1password",
        "lastpass",
        "keepasshttp",
        "enpass",
        "logonui",
        "consent"
    ];

    public bool FirstRun { get; set; } = true;

    public void Normalize()
    {
        if (PopupDurationSeconds < 2)
        {
            PopupDurationSeconds = 2;
        }

        if (PopupDurationSeconds > 30)
        {
            PopupDurationSeconds = 30;
        }

        if (MinWordLength < 1)
        {
            MinWordLength = 1;
        }

        if (string.IsNullOrWhiteSpace(Language))
        {
            Language = "en_US";
        }

        ExcludedProcesses ??= [];
        for (var i = 0; i < ExcludedProcesses.Count; i++)
        {
            ExcludedProcesses[i] = (ExcludedProcesses[i] ?? string.Empty).Trim().ToLowerInvariant();
        }

        ExcludedProcesses = ExcludedProcesses
            .Where(static name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool IsProcessExcluded(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        return ExcludedProcesses.Any(excluded =>
            string.Equals(excluded, name, StringComparison.OrdinalIgnoreCase));
    }
}
