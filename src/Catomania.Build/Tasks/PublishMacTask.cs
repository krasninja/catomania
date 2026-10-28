using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Publishes the application for macOS, bundles it with Velopack and wraps the bundle into a .dmg.
/// </summary>
[TaskName("Publish-Mac")]
[TaskDescription("Publish Mac project")]
public sealed class PublishMacTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.CleanDirectory(context.OutputDirectory);
        context.DotNetPublish(
            context.Directory("./src/Catomania/"),
            new BaseDotNetPublishSettings(context)
            {
                Runtime = context.GetRuntimeIdentifier(ApplicationConstants.PlatformMacOS),
            });

        var velopackArgs = context.GetVelopackArguments(ApplicationConstants.PlatformMacOS);
        context.RunVelopack(velopackArgs);

        // Runs last: the bundle has to exist before it can be wrapped into a disk image.
        MacDiskImage.Create(context);

        return Task.CompletedTask;
    }
}
