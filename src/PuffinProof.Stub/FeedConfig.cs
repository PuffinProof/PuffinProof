using System.IO;
using System.Text.Json;

namespace PuffinProof.Stub;

public sealed class FeedConfig
{
    public string GithubRepo { get; set; } = "";

    public string AssetName { get; set; } = "PuffinProof.msix";

    public static FeedConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "feed.json");
        var config = new FeedConfig();
        try
        {
            if (File.Exists(path))
            {
                config = JsonSerializer.Deserialize<FeedConfig>(File.ReadAllText(path), JsonOptions) ?? config;
            }
        }
        catch
        {
            // Keep defaults.
        }

        var env = Environment.GetEnvironmentVariable("PUFFINPROOF_GITHUB_REPO");
        if (!string.IsNullOrWhiteSpace(env))
        {
            config.GithubRepo = env.Trim();
        }

        return config;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
