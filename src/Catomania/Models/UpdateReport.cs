namespace Catomania.Models;

/// <summary>
/// Outcome of an application update check.
/// </summary>
public enum UpdateReportKind
{
    /// <summary>The release feed is reachable but has no releases yet.</summary>
    NoReleases,

    /// <summary>The application is up to date.</summary>
    UpToDate,

    /// <summary>A newer version is available but the application is not installed by Velopack.</summary>
    NotInstalled,

    /// <summary>An update is downloaded and being applied, the application exits or restarts.</summary>
    Updating,

    /// <summary>The update check failed.</summary>
    Error,
}

/// <summary>
/// Result of an application update check.
/// </summary>
/// <param name="Kind">Outcome kind.</param>
/// <param name="Message">Human readable message.</param>
public sealed record UpdateReport(UpdateReportKind Kind, string Message);
