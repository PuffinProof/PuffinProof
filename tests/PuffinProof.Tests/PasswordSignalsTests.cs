using PuffinProof.Core;

namespace PuffinProof.Tests;

public class PasswordSignalsTests
{
    [Theory]
    [InlineData("Password")]
    [InlineData("confirm password")]
    [InlineData("passwd")]
    [InlineData("PIN")]
    [InlineData("one-time code")]
    [InlineData("Sign in to your account")]
    [InlineData("Unlock Windows")]
    [InlineData("cvv")]
    public void Flags_secret_ui(string text) => Assert.True(PasswordSignals.LooksSecret(text));

    [Theory]
    [InlineData("Subject")]
    [InlineData("shopping")]
    [InlineData("mapping")]
    [InlineData("passenger")]
    [InlineData("Hello world")]
    public void Allows_ordinary_ui(string text) => Assert.False(PasswordSignals.LooksSecret(text));
}
