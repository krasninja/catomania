using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Publishes the application for macOS and bundles it with Velopack.
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
                Runtime = DotNetConstants.RidMacOSX64,
            });

        var velopackArgs = context.GetVelopackArguments(ApplicationConstants.PlatformMacOS);
        context.RunVelopack(velopackArgs);

        return Task.CompletedTask;
    }
}
