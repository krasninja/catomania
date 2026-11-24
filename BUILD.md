# Build

## Build & Run

Requires .NET 11.

```bash
dotnet build ./src/Catomania.sln
dotnet run --project ./src/Catomania
dotnet test --project ./src/Catomania.Tests
```

## Build & Publish Installers

The repository contains a Cake Frosting build script (`./build.sh`) that publishes the application as a [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) binary and packs it with [Velopack](https://velopack.io/):

```bash
./build.sh --target=Publish-Linux
./build.sh --target=Publish-Windows
./build.sh --target=Publish-Mac
```

Published binaries are placed to `./output/publish`, Velopack releases (setup, portable package and the update feed) to `./output/releases/<channel>`. The version is calculated by GitVersion and can be overridden with the `CATOMANIA_VERSION` environment variable (for example, `CATOMANIA_VERSION=1.2.3 ./build.sh --target=Publish-Linux`).

Native AOT is enabled by default. To publish a regular self-contained binary instead, pass `--PublishAot=false`, for example `./build.sh --target=Publish-Linux --PublishAot=false`.

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

## macOS code signing and notarization

By default `Publish-Mac` seals the `.app` bundle with an ad-hoc signature only, which Gatekeeper blocks for downloaded apps. Configuring a **Developer ID Application** certificate switches it to a real signature with the hardened runtime and a secure timestamp; adding Apple ID credentials also notarizes the bundle and the `.dmg` and staples the tickets, so users can open the download without any warning. Notarization is strongly recommended: since macOS Catalina, Gatekeeper blocks even Developer ID signed downloads that are not notarized.

The build reads everything from environment variables (implemented in `src/Catomania.Build/MacCodeSign.cs`):

| Variable | Meaning |
| --- | --- |
| `CATOMANIA_MAC_CERT_FILE` | Path to the exported `.p12` certificate with its private key (local builds). |
| `CATOMANIA_MAC_CERT_BASE64` | Base64 of the same `.p12` (CI), used when no file is set. |
| `CATOMANIA_MAC_CERT_PASSWORD` | The `.p12` export password; may be empty or unset for a password-less export. |
| `CATOMANIA_MAC_SIGN_IDENTITY` | Optional identity override; detected from the certificate when omitted. |
| `CATOMANIA_MAC_NOTARY_APPLE_ID` | Apple ID e-mail address, enables notarization together with the next two. |
| `CATOMANIA_MAC_NOTARY_PASSWORD` | An [app-specific password](https://account.apple.com/account/manage) for that Apple ID (not the account password). |
| `CATOMANIA_MAC_NOTARY_TEAM_ID` | Apple Developer team identifier, `3HWMVVYBA5` for this project's certificate. |

The certificate is imported into a temporary keychain that is deleted after the build, the login keychain is never modified. On a Mac with the certificate at `~/Downloads/ikozhin_cert.p12` (exported without a password):

```bash
CATOMANIA_MAC_CERT_FILE=~/Downloads/ikozhin_cert.p12 ./build.sh --target=Publish-Mac
# with notarization:
CATOMANIA_MAC_CERT_FILE=~/Downloads/ikozhin_cert.p12 \
CATOMANIA_MAC_NOTARY_APPLE_ID=you@example.com \
CATOMANIA_MAC_NOTARY_PASSWORD=xxxx-xxxx-xxxx-xxxx \
CATOMANIA_MAC_NOTARY_TEAM_ID=3HWMVVYBA5 \
./build.sh --target=Publish-Mac
```

### GitHub secrets

`release.yml` passes the secrets below to the build; signing stays disabled (ad-hoc only) while they are absent. Set them with the [GitHub CLI](https://cli.github.com/) from the repository root:

```bash
# The certificate, base64 encoded. -w0 keeps it on one line (on macOS use: base64 -i <file>).
base64 -w0 ~/Downloads/ikozhin_cert.p12 > /tmp/catomania-cert.b64
gh secret set MAC_CERT_BASE64 < /tmp/catomania-cert.b64
rm /tmp/catomania-cert.b64

# Only needed when the .p12 was exported WITH a password; this certificate has none.
# gh secret set MAC_CERT_PASSWORD --body 'the-export-password'

# Notarization (recommended). Create the app-specific password at
# https://account.apple.com/account/manage (Sign-In and Security > App-Specific Passwords).
gh secret set MAC_NOTARY_APPLE_ID --body 'you@example.com'
gh secret set MAC_NOTARY_PASSWORD --body 'xxxx-xxxx-xxxx-xxxx'
gh secret set MAC_NOTARY_TEAM_ID --body '3HWMVVYBA5'

# Optional: pin the identity instead of auto-detection.
# gh secret set MAC_SIGN_IDENTITY --body 'Developer ID Application: Vladimir Kozhin (3HWMVVYBA5)'
```

Notes:

- The certificate must be of type **Developer ID Application** (distribution outside the App Store), exported from Keychain Access together with its private key. Verify with `security find-identity -v -p codesigning` after importing it on a Mac.
- The Apple ID used for notarization must belong to (or be a member of) the team that issued the certificate.
- Never commit the `.p12` or the passwords to the repository; the temporary keychain and decoded certificate file are deleted by the build itself.
- The certificate expires on 2027-12-24; renew it in the Apple Developer account and update `MAC_CERT_BASE64` before that.

## Continuous Integration

Two GitHub workflows live in `.github/workflows`:

- **`ci.yml`** runs on every push and pull request: it builds and tests the solution, compiles on all three platforms and runs the real per-platform packaging (the same `Publish-Linux` / `Publish-Windows` / `Publish-Mac` Cake targets as the release workflow), producing a Native AOT binary packed by Velopack. The packaging job fails on any trim or AOT analyzer warning, checks that the Skia and HarfBuzz native libraries were deployed next to the binary and runs the `--update-check` path. The uploaded `velopack-<channel>` artifacts contain the ready-to-deploy Velopack files: the installers (`Setup.exe`, `.AppImage`, `.dmg`), the full and delta packages (`.nupkg`) and the update feeds.
- **`release.yml`** runs when a semver tag is pushed. It packages every platform in parallel, creates the GitHub release and uploads the installers, the portable packages and the update feed. Before packing it downloads the previous release into `./output/releases/<channel>` so Velopack can build the delta package and keep the full feed: without that step the feed would only list the new version.

Windows code signing is wired through the `VPK_*` environment variables Velopack reads natively and stays disabled while the corresponding repository secrets are empty. macOS signing and notarization are implemented in `src/Catomania.Build/MacCodeSign.cs` and configured with the `MAC_*` repository secrets, see [macOS code signing](#macos-code-signing-and-notarization). Unsigned Windows builds show the SmartScreen warning and unsigned macOS builds are blocked by Gatekeeper. The CI workflow (`ci.yml`) always builds unsigned: repository secrets are not available to pull requests from forks.

## Updates

The application reads the Velopack release feed from the assets of the latest GitHub release, `https://github.com/krasninja/catomania/releases/latest/download/releases.<channel>.json`, where `<channel>` is `win`, `linux`, `osx` or `osx-arm64`. Every release has to carry the feeds and packages of all channels, which `release.yml` takes care of; pre-releases are not offered as updates. The macOS builds are only bundled (`vpk bundle`), not packed, so they cannot update themselves yet.

- Right-click the animal and choose **Check for Updates**.
- Or run `Catomania --update-check`, optionally with `--update-url <uri>` to use another feed. The command prints the check result and exits, applying the update when one is available.

Self-update works when the application is installed by Velopack: on Linux run the packed `catomania.AppImage` (the updater replaces the AppImage file in place), on Windows run the installed setup. Development and portable builds only report the available version.
