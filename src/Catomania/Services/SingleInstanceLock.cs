namespace Catomania.Services;

/// <summary>
/// Makes sure only one animal runs per user. Two instances would fight over the settings file and,
/// on KDE Plasma, unload each other's cursor tracking script.
/// </summary>
/// <remarks>
/// The lock is an exclusively opened file in the settings directory. .NET takes an OS lock for
/// <see cref="FileShare.None" /> on every platform (<c>flock</c> on Linux and macOS), and the OS
/// releases it when the process dies, so a crash never leaves a stale lock behind.
/// </remarks>
public sealed class SingleInstanceLock : IDisposable
{
    /// <summary>
    /// Lock file name.
    /// </summary>
    public const string FileName = "instance.lock";

    private readonly FileStream? _stream;

    private SingleInstanceLock(FileStream? stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Try to become the only running instance.
    /// </summary>
    /// <param name="directoryPath">Directory to keep the lock file in, the settings one when not set.</param>
    /// <returns>The held lock, or <c>null</c> when another instance holds it already.</returns>
    public static SingleInstanceLock? TryAcquire(string? directoryPath = null)
    {
        directoryPath ??= SettingsStorage.GetDefaultDirectoryPath();
        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // An unusable configuration directory should not keep the animal from starting.
            Console.Error.WriteLine($"Catomania: cannot create {directoryPath}, running without the instance lock: {e.Message}");
            return new SingleInstanceLock(null);
        }

        try
        {
            var stream = new FileStream(
                Path.Combine(directoryPath, FileName),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
            return new SingleInstanceLock(stream);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException e)
        {
            Console.Error.WriteLine($"Catomania: cannot open the instance lock, running without it: {e.Message}");
            return new SingleInstanceLock(null);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _stream?.Dispose();
}
