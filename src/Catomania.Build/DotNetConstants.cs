// ReSharper disable InconsistentNaming

namespace Catomania.Build;

/// <summary>
/// .NET related constants.
/// </summary>
public static class DotNetConstants
{
    #region Target

    /// <summary>Release configuration name.</summary>
    public const string ConfigurationRelease = "Release";

    /// <summary>Debug configuration name.</summary>
    public const string ConfigurationDebug = "Debug";

    #endregion

    // Runtime identifiers are not listed here on purpose. They are the platform identifier from
    // <see cref="ApplicationConstants" /> and the architecture of the build combined by
    // <see cref="BuildContext.GetRuntimeIdentifier" />, so a single definition of both is enough.
    // The catalog of valid values is at https://docs.microsoft.com/en-us/dotnet/core/rid-catalog.
}
