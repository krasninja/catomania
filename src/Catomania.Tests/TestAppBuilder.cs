using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace Catomania.Tests;

/// <summary>
/// Builds the headless Avalonia application used by the rendering tests.
/// </summary>
public sealed class TestAppBuilder
{
    /// <summary>
    /// Build the Avalonia application.
    /// </summary>
    /// <returns>Application builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            });
}

/// <summary>
/// The single headless session shared by all tests that need the UI thread. Two sessions
/// running in parallel interfere with each other and make the rendering tests flaky.
/// </summary>
public static class TestHeadlessSession
{
    /// <summary>
    /// Gets the shared headless unit test session.
    /// </summary>
    public static readonly HeadlessUnitTestSession Instance =
        HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
}

/// <summary>
/// Minimal application used by the headless tests.
/// </summary>
public sealed class TestApp : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}
