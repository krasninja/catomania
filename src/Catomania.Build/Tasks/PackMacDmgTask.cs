using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Creates the macOS disk image on its own, without publishing first.
/// The usual entry point is <c>Publish-Mac</c>, which creates the image as its last step.
/// </summary>
[TaskName("Pack-Mac-Dmg")]
[TaskDescription("Create a macOS .dmg from the already published application bundle")]
public sealed class PackMacDmgTask : FrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override void Run(BuildContext context) => MacDiskImage.Create(context);
}
