using System.Text.RegularExpressions;

namespace JustSpell.Core;

/// <summary>
/// Conservative "this looks like a secret field" checks. Used before any
/// UI Automation text read so passwords never enter our buffers.
/// </summary>
public static class PasswordSignals
{
    private static readonly string[] PhraseCues =
    [
        "password",
        "passwd",
        "passcode",
        "passphrase",
        "pass-phrase",
        "secret",
        "otp",
        "totp",
        "2fa",
        "two-factor",
        "two factor",
        "one-time code",
        "one time code",
        "cvv",
        "cvc",
        "ssn",
        "social security",
        "sign in",
        "sign-in",
        "signin",
        "log in",
        "login",
        "unlock",
        "credentials",
        "authenticator"
    ];

    private static readonly Regex ShortToken = new(
        @"\b(pin|pwd|pw)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool LooksSecret(params string?[] parts)
    {
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var text = part.ToLowerInvariant();
            foreach (var cue in PhraseCues)
            {
                if (text.Contains(cue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            if (ShortToken.IsMatch(part))
            {
                return true;
            }
        }

        return false;
    }
}
