using Cake.Frosting;

namespace Catomania.Build;

/// <summary>
/// Entry point of the build script.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the Cake Frosting build host.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Process exit code.</returns>
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}
