using System.Reflection;

namespace PuffinProof.Core;

public static class AppVersion
{
    public static string Display =>
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "0.0.0.0";
}
