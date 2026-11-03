using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Catomania.Localization;

public static partial class Strings
{
    public static string Pause => Get();

    public static string Animal => Get();

    public static string Cat => Get();

    public static string Panda => Get();

    public static string Dog => Get();

    public static string Snake => Get();

    public static string Parrot => Get();

    public static string Pig => Get();

    public static string Frog => Get();

    public static string Capybara => Get();

    public static string Speed => Get();

    public static string Slow => Get();

    public static string Normal => Get();

    public static string Fast => Get();

    public static string Fur => Get();

    public static string Feathers => Get();

    public static string Skin => Get();

    public static string Ginger => Get();

    public static string Gray => Get();

    public static string Black => Get();

    public static string Cream => Get();

    public static string LiverAndWhite => Get();

    public static string BlackAndWhite => Get();

    public static string RedAndWhite => Get();

    public static string LemonAndWhite => Get();

    public static string Mint => Get();

    public static string DarkGreen => Get();

    public static string Blue => Get();

    public static string Pink => Get();

    public static string Yellow => Get();

    public static string Scarlet => Get();

    public static string BlueAndGold => Get();

    public static string Green => Get();

    public static string Hyacinth => Get();

    public static string Size => Get();

    public static string Small => Get();

    public static string Medium => Get();

    public static string Large => Get();

    public static string Configuration => Get();

    public static string LaunchAtStartup => Get();

    public static string AlwaysOnTop => Get();

    public static string Recenter => Get();

    public static string CheckForUpdates => Get();

    public static string Help => Get();

    public static string Website => Get();

    public static string About => Get();

    public static string Quit => Get();

    public static string QuitCatomania => Get();

    public static string Tricks => Get();

    public static string Walk => Get();

    public static string LookAround => Get();

    public static string Hop => Get();

    public static string Speak => Get();

    public static string Emote => Get();

    public static string Wiggle => Get();

    public static string Shake => Get();

    public static string BunnyHops => Get();

    public static string Spin => Get();

    public static string Zoomies => Get();

    public static string AboutCatomania => Get();

    public static string Version => Get();

    public static string Platform => Get();

    public static string CatomaniaUpdates => Get();

    public static string UpdateCheckFailedAt => Get();

    public static string NoReleases => Get();

    public static string UpToDate => Get();

    public static string UpdateAvailableNotInstalled => Get();

    public static string Updating => Get();

    public static string UpdateApplyFailed => Get();

    public static string FailedToCheckUpdates => Get();

    public static string Information => Get();

    public static string Error => Get();

    public static string Ok => Get();

    public static string Yes => Get();

    public static string No => Get();

    public static string Cancel => Get();

    public static string Abort => Get();

    public static string Retry => Get();

    public static string Ignore => Get();

    private static readonly ResourceManager Resources = new(
        "Catomania.Resources.Strings",
        typeof(Strings).Assembly);

    public static string Get(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(culture);
        return Resources.GetString(key, culture) ?? string.Empty;
    }

    public static string Format(string format, params object?[] args)
        => string.Format(CultureInfo.CurrentUICulture, format, args);

    private static string Get([CallerMemberName] string key = "")
        => Get(key, CultureInfo.CurrentUICulture);
}
