using Catomania.Models;
using Catomania.Services;
using Xunit;

namespace Catomania.Tests.Services;

/// <summary>
/// Tests for <see cref="SettingsStorage" />.
/// </summary>
public sealed class SettingsStorageTests : IDisposable
{
    private readonly string _directoryPath;

    public SettingsStorageTests()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), "catomania-tests-" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Load_MissingFile_ShouldReturnDefaultSettings()
    {
        // Arrange.
        var storage = new SettingsStorage(_directoryPath);

        // Act.
        var settings = storage.Load();

        // Assert.
        Assert.Equal(AnimalSpeed.Normal, settings.Speed);
        Assert.Equal(CatColorScheme.Ginger, settings.ColorScheme);
        Assert.Equal(AnimalSize.Medium, settings.Size);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.Paused);
    }

    [Fact]
    public void Load_BrokenFile_ShouldReturnDefaultSettings()
    {
        // Arrange.
        var storage = new SettingsStorage(_directoryPath);
        Directory.CreateDirectory(_directoryPath);
        File.WriteAllText(storage.FilePath, "this is not a JSON");

        // Act.
        var settings = storage.Load();

        // Assert.
        Assert.Equal(AnimalSpeed.Normal, settings.Speed);
    }

    [Fact]
    public void SaveAndLoad_ChangedSettings_ShouldRoundTrip()
    {
        // Arrange.
        var storage = new SettingsStorage(_directoryPath);
        var settings = new AppSettings
        {
            Speed = AnimalSpeed.Fast,
            ColorScheme = CatColorScheme.Black,
            Size = AnimalSize.Large,
            AlwaysOnTop = false,
            Paused = true,
            SleepAfterSeconds = 7,
        };

        // Act.
        storage.Save(settings);
        var loaded = storage.Load();

        // Assert.
        Assert.Equal(AnimalSpeed.Fast, loaded.Speed);
        Assert.Equal(CatColorScheme.Black, loaded.ColorScheme);
        Assert.Equal(AnimalSize.Large, loaded.Size);
        Assert.False(loaded.AlwaysOnTop);
        Assert.True(loaded.Paused);
        Assert.Equal(7, loaded.SleepAfterSeconds);
    }

    [Fact]
    public void Save_Twice_ShouldOverwriteAndLeaveNoTemporaryFiles()
    {
        // Arrange.
        var storage = new SettingsStorage(_directoryPath);

        // Act.
        storage.Save(new AppSettings { Size = AnimalSize.Large });
        storage.Save(new AppSettings { Size = AnimalSize.Small });

        // Assert.
        Assert.Equal(AnimalSize.Small, storage.Load().Size);
        Assert.Equal([storage.FilePath], Directory.GetFiles(_directoryPath));
    }

    [Fact]
    public void Save_ShouldWriteEnumValuesAsStrings()
    {
        // Arrange.
        var storage = new SettingsStorage(_directoryPath);

        // Act.
        storage.Save(new AppSettings { Speed = AnimalSpeed.Slow });
        var contents = File.ReadAllText(storage.FilePath);

        // Assert.
        Assert.Contains("\"slow\"", contents, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("speed", contents, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetDefaultDirectoryPath_ShouldUseTheCatomaniaDirectory()
    {
        // Act.
        var directoryPath = SettingsStorage.GetDefaultDirectoryPath();

        // Assert.
        Assert.Equal("Catomania", Path.GetFileName(directoryPath));
        Assert.True(Path.IsPathRooted(directoryPath));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, true);
        }
    }
}
