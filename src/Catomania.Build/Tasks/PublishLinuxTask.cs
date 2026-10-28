using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Publishes the application for Linux and packs it with Velopack.
/// </summary>
[TaskName("Publish-Linux")]
[TaskDescription("Publish Linux project")]
public sealed class PublishLinuxTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.CleanDirectory(context.OutputDirectory);
        context.DotNetPublish(
            context.Directory("./src/Catomania/"),
            new BaseDotNetPublishSettings(context)
            {
                Runtime = context.GetRuntimeIdentifier(ApplicationConstants.PlatformLinux),
            });

        var velopackArgs = context.GetVelopackArguments(ApplicationConstants.PlatformLinux);
        velopackArgs = velopackArgs.Concat(["--categories", "Utility"]).ToArray();
        context.RunVelopack(velopackArgs);

        return Task.CompletedTask;
    }
}
