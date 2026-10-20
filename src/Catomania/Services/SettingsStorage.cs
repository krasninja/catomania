using System.Text.Json;
using Catomania.Models;

namespace Catomania.Services;

/// <summary>
/// Loads and saves the application settings as a JSON file in the user configuration directory.
/// </summary>
public sealed class SettingsStorage
{
    /// <summary>
    /// Settings file name.
    /// </summary>
    public const string FileName = "settings.json";

    /// <summary>
    /// Application directory name inside the user configuration directory.
    /// </summary>
    public const string DirectoryName = "Catomania";

    private readonly string _directoryPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStorage" /> class.
    /// </summary>
    /// <param name="directoryPath">Directory to store the settings in, the default one is used when not set.</param>
    public SettingsStorage(string? directoryPath = null)
    {
        _directoryPath = directoryPath ?? GetDefaultDirectoryPath();
    }

    /// <summary>
    /// Gets the full path of the settings file.
    /// </summary>
    public string FilePath => Path.Combine(_directoryPath, FileName);

    /// <summary>
    /// Get the directory the settings are stored in by default.
    /// </summary>
    /// <returns>Directory path.</returns>
    public static string GetDefaultDirectoryPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DirectoryName);

    /// <summary>
    /// Load the settings. Default settings are returned when the file does not exist or cannot be read.
    /// </summary>
    /// <returns>Application settings.</returns>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize(stream, CatomaniaJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine($"Catomania: cannot read {FilePath}: {e.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Save the settings.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_directoryPath);

            // Write to a temporary file first, so an interrupted save does not leave a truncated settings file.
            var temporaryFilePath = FilePath + ".tmp";
            using (var stream = File.Create(temporaryFilePath))
            {
                JsonSerializer.Serialize(stream, settings, CatomaniaJsonContext.Default.AppSettings);
            }

            File.Move(temporaryFilePath, FilePath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Catomania: cannot save {FilePath}: {e.Message}");
        }
    }
}
