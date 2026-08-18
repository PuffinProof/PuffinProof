using PuffinProof.Core;

namespace PuffinProof.Tests;

public class VersionCompareTests
{
    [Fact]
    public void Missing_install_means_newer() =>
        Assert.True(VersionCompare.IsNewer("1.0.1", null));

    [Fact]
    public void Same_version_is_not_newer() =>
        Assert.False(VersionCompare.IsNewer("1.0.0", "1.0.0"));

    [Fact]
    public void Tag_prefix_is_ignored() =>
        Assert.False(VersionCompare.IsNewer("v1.0.0", "1.0.0"));

    [Fact]
    public void Higher_tag_is_newer() =>
        Assert.True(VersionCompare.IsNewer("v1.2.0", "1.1.9"));
}
