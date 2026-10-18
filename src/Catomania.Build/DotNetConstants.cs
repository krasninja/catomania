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

    #region .NET RID Catalog

    // https://docs.microsoft.com/en-us/dotnet/core/rid-catalog.

    /// <summary>Linux x64 runtime identifier.</summary>
    public const string RidLinuxX64 = $"{ApplicationConstants.PlatformLinux}-{ApplicationConstants.ArchitectureX64}";

    /// <summary>Linux ARM64 runtime identifier.</summary>
    public const string RidLinuxArm64 = $"{ApplicationConstants.PlatformLinux}-{ApplicationConstants.ArchitectureArm64}";

    /// <summary>Windows x64 runtime identifier.</summary>
    public const string RidWindowsX64 = $"{ApplicationConstants.PlatformWindows}-{ApplicationConstants.ArchitectureX64}";

    /// <summary>macOS x64 runtime identifier.</summary>
    public const string RidMacOSX64 = $"{ApplicationConstants.PlatformMacOS}-{ApplicationConstants.ArchitectureX64}";

    /// <summary>macOS ARM64 runtime identifier.</summary>
    public const string RidMacOSArm64 = $"{ApplicationConstants.PlatformMacOS}-{ApplicationConstants.ArchitectureArm64}";

    #endregion
}
