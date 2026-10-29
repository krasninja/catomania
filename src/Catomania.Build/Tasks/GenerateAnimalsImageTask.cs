using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Catomania.Build.Tasks;

/// <summary>
/// Draws all animals on green grass and saves the picture for the README.
/// </summary>
[TaskName("Generate-Animals-Image")]
[TaskDescription("Draw all animals on green grass and save the picture as a PNG")]
public sealed class GenerateAnimalsImageTask : FrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override void Run(BuildContext context)
    {
        var path = context.MakeAbsolute(context.AnimalsImageFile).FullPath;
        AnimalsImage.Create(path);
        context.Information($"The animals image is saved to {path}.");
    }
}
