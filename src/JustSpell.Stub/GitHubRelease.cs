using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JustSpell.Stub;

public sealed class GitHubRelease
{
    public required string Tag { get; init; }

    public required string Version { get; init; }

    public required string DownloadUrl { get; init; }

    public string? Digest { get; init; }

    public static async Task<GitHubRelease?> FetchLatestAsync(FeedConfig feed, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(feed.GithubRepo) || !feed.GithubRepo.Contains('/'))
        {
            return null;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JustSpellSetup", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var url = "https://api.github.com/repos/" + feed.GithubRepo.Trim() + "/releases/latest";
        using var response = await client.GetAsync(url, token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var version = tag.TrimStart('v', 'V');
        if (!root.TryGetProperty("assets", out var assets))
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString();
            if (!string.Equals(name, feed.AssetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var download = asset.GetProperty("browser_download_url").GetString();
            if (string.IsNullOrWhiteSpace(download))
            {
                continue;
            }

            string? digest = null;
            if (asset.TryGetProperty("digest", out var digestEl))
            {
                digest = digestEl.GetString();
            }

            return new GitHubRelease
            {
                Tag = tag,
                Version = version,
                DownloadUrl = download,
                Digest = digest
            };
        }

        return null;
    }

    public static async Task<string> DownloadAsync(string url, IProgress<double>? progress, CancellationToken token)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JustSpellSetup", "1.0"));
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var dest = Path.Combine(Path.GetTempPath(), "JustSpell-" + Guid.NewGuid().ToString("n") + ".msix");
        var total = response.Content.Headers.ContentLength ?? -1;
        await using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using var output = File.Create(dest);
        var buffer = new byte[81_920];
        long read = 0;
        int n;
        while ((n = await input.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, n), token).ConfigureAwait(false);
            read += n;
            if (total > 0)
            {
                progress?.Report(read / (double)total);
            }
        }

        return dest;
    }
}
