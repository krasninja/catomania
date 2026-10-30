# Build

## Build & Run

Requires .NET 10.

```bash
dotnet build ./src/Catomania.sln
dotnet run --project ./src/Catomania
dotnet test ./src/Catomania.Tests
```

## Build & Publish Installers

The repository contains a Cake Frosting build script (`./build.sh`) that publishes the application as a [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) binary and packs it with [Velopack](https://velopack.io/):

```bash
./build.sh --target=Publish-Linux
./build.sh --target=Publish-Windows
./build.sh --target=Publish-Mac
```

Published binaries are placed to `./output/publish`, Velopack releases (setup, portable package and the update feed) to `./output/releases/<channel>`. The version is calculated by GitVersion and can be overridden with the `CATOMANIA_VERSION` environment variable (for example, `CATOMANIA_VERSION=1.2.3 ./build.sh --target=Publish-Linux`).

The architecture defaults to `x64` and is set with the `CATOMANIA_ARCH` environment variable or the `--arch` argument, for example `CATOMANIA_ARCH=arm64 ./build.sh --target=Publish-Mac`. x64 publishes to the plain channel (`win`, `linux`, `osx`), any other architecture to a channel of its own (`osx-arm64`), which is also the feed path the application reads.

Prerequisites:

- The Velopack CLI: `dotnet tool install -g vpk`.
- GitVersion for the version calculation: `dotnet tool install -g gitversion.tool`.
- The Native AOT toolchain of the target platform:

  | OS | Prerequisites |
  | --- | --- |
  | Linux | `sudo apt install clang zlib1g-dev`, plus `squashfs-tools` for the AppImage package |
  | Windows | Visual Studio 2022 or later with the *Desktop development with C++* workload |
  | macOS | The Xcode command line tools, plus `hdiutil` and `ditto` for the `.dmg` |

Native AOT cannot cross compile, so every target has to be built on a machine with the matching operating system **and** CPU architecture: `osx-x64` needs an Intel Mac, `osx-arm64` an Apple Silicon one.

`Publish-Mac` also wraps the `.app` bundle into a `.dmg` as its last step, using `hdiutil`. That step is skipped with a message anywhere but on macOS. To rebuild only the disk image from an already published bundle, run `./build.sh --target=Pack-Mac-Dmg`.

`./build.sh --target=Generate-Animals-Image` draws all animals on green grass with the application's own rendering code and saves the picture to `./output/animals.png`.

## Continuous Integration

Two GitHub workflows live in `.github/workflows`:

- **`ci.yml`** runs on every push and pull request: it builds and tests the solution, compiles on all three platforms and runs the real per-platform packaging (the same `Publish-Linux` / `Publish-Windows` / `Publish-Mac` Cake targets as the release workflow), producing a Native AOT binary packed by Velopack. The packaging job fails on any trim or AOT analyzer warning, checks that the Skia and HarfBuzz native libraries were deployed next to the binary and runs the `--update-check` path. The uploaded `velopack-<channel>` artifacts contain the ready-to-deploy Velopack files: the installers (`Setup.exe`, `.AppImage`, `.dmg`), the full and delta packages (`.nupkg`) and the update feeds.
- **`release.yml`** runs when a semver tag is pushed. It packages every platform in parallel, creates the GitHub release and uploads the installers, the portable packages and the update feed. Before packing it downloads the previous release into `./output/releases/<channel>` so Velopack can build the delta package and keep the full feed: without that step the feed would only list the new version.

Code signing is wired through the `VPK_*` environment variables Velopack reads natively and stays disabled while the corresponding repository secrets are empty. Unsigned Windows builds show the SmartScreen warning and unsigned macOS builds are blocked by Gatekeeper.

## Updates

The application checks the Velopack release feed at `https://catomania.anti-soft.ru/<platform>`, where `<platform>` is `win`, `linux` or `osx`:

- Right-click the cat and choose **Check for Updates**.
- Or run `Catomania --update-check`, optionally with `--update-url <uri>` to use another feed. The command prints the check result and exits, applying the update when one is available.

Self-update works when the application is installed by Velopack: on Linux run the packed `catomania.AppImage` (the updater replaces the AppImage file in place), on Windows run the installed setup. Development and portable builds only report the available version.
