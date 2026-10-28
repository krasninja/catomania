using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Publishes the application for Windows and packs it with Velopack.
/// </summary>
[TaskName("Publish-Windows")]
[TaskDescription("Publish Windows project")]
public sealed class PublishWindowsTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.CleanDirectory(context.OutputDirectory);
        context.DotNetPublish(
            context.Directory("./src/Catomania/"),
            new BaseDotNetPublishSettings(context)
            {
                Runtime = context.GetRuntimeIdentifier(ApplicationConstants.PlatformWindows),
            });

        var velopackArgs = context.GetVelopackArguments(ApplicationConstants.PlatformWindows);
        context.RunVelopack(velopackArgs);

        return Task.CompletedTask;
    }
}
