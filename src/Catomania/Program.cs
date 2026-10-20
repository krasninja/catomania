using Avalonia;
using Catomania.Models;
using Catomania.Services;
using Velopack;

namespace Catomania;

class Program
{
    private const string UpdateCheckArgument = "--update-check";
    private const string UpdateUrlArgument = "--update-url";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Velopack hooks for installed applications: updates, restarts and shortcuts.
        VelopackApp.Build().Run();

        if (Array.Exists(args, arg => arg == UpdateCheckArgument))
        {
            return RunUpdateCheck(args);
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>
    /// Check the release feed for updates and apply them, then exit. Used by scripts and tests.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Process exit code.</returns>
    private static int RunUpdateCheck(string[] args)
    {
        string? updateUrl = null;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == UpdateUrlArgument)
            {
                updateUrl = args[i + 1];
            }
        }

        var report = new AppUpdateService(updateUrl)
            .CheckAndUpdateAsync(restartAfterUpdate: false, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        Console.WriteLine(report.Message);
        return report.Kind == UpdateReportKind.Error ? 1 : 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
