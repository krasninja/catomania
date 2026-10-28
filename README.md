Catomania
=========

Catomania is a small Avalonia desktop for those who wants to add a cat into your screen! A kitty chases the mouse cursor across the screen.

## Features

1. Runs locally on Windows, macOS and Linux (X11, KDE Plasma and GNOME on Wayland, see [Linux and Wayland](#linux-and-wayland)).
2. The kitty walks and runs towards the cursor, stops nearby, looks around and falls asleep when the cursor does not move for a while.
3. The cat is drawn with vector shapes, so there are no image assets and it stays sharp at any size.
4. Three animals: a cat with four fur colors (ginger, gray, black and cream), a panda cub and a springer spaniel.
5. Drag the cat with the left mouse button, double-click it to pause, scroll the mouse wheel over it to change its size.
6. Right-click the cat for the menu: pause, animal, speed, fur, size, always on top, recenter, check for updates and quit.
7. Settings are saved between runs in the user configuration directory.
8. Self-updates from the Velopack release feed when installed.

| Idle | Walk | Run | Sleep | Dragged |
| --- | --- | --- | --- | --- |
| ![Idle](docs/screenshots/cat-idle.png) | ![Walk](docs/screenshots/cat-walk.png) | ![Run](docs/screenshots/cat-run.png) | ![Sleep](docs/screenshots/cat-sleep.png) | ![Dragged](docs/screenshots/cat-dragged.png) |

| Ginger | Gray | Black | Cream |
| --- | --- | --- | --- |
| ![Ginger](docs/screenshots/cat-fur-ginger.png) | ![Gray](docs/screenshots/cat-fur-gray.png) | ![Black](docs/screenshots/cat-fur-black.png) | ![Cream](docs/screenshots/cat-fur-cream.png) |

## Build & Run

Requires .NET 10.

```bash
dotnet build ./src/Catomania.sln
dotnet run --project ./src/Catomania
dotnet test ./src/Catomania.Tests
```

## Linux and Wayland

Wayland does not let applications read the global cursor position, and XWayland knows it only while the cursor is over an X11 window, that is, over the cat. So on Wayland Catomania asks the compositor:

- **KDE Plasma**: a small KWin script is loaded over D-Bus when the cat starts and unloaded when it quits. Nothing has to be installed.
- **GNOME**: the `catomania-cursor@catomania` GNOME Shell extension is installed into `~/.local/share/gnome-shell/extensions` and enabled on the first run. GNOME Shell on Wayland picks up new extensions only on login, so **log out and back in once** after the first run. If the cat still follows the cursor only nearby, check that the extension is enabled in the Extensions application. To remove it, run `gnome-extensions uninstall catomania-cursor@catomania`.

The extension and the script share only the cursor position. On other Wayland compositors the cat sees the cursor only when it is close by.

## Build & publish installers

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

## Continuous integration

Two GitHub workflows live in `.github/workflows`:

- **`ci.yml`** runs on every push and pull request: it builds and tests the solution, compiles on all three platforms and runs the real per-platform packaging (the same `Publish-Linux` / `Publish-Windows` / `Publish-Mac` Cake targets as the release workflow), producing a Native AOT binary packed by Velopack. The packaging job fails on any trim or AOT analyzer warning, checks that the Skia and HarfBuzz native libraries were deployed next to the binary and runs the `--update-check` path. The uploaded `velopack-<channel>` artifacts contain the ready-to-deploy Velopack files: the installers (`Setup.exe`, `.AppImage`, `.dmg`), the full and delta packages (`.nupkg`) and the update feeds.
- **`release.yml`** runs when a semver tag is pushed. It packages every platform in parallel, creates the GitHub release and uploads the installers, the portable packages and the update feed. Before packing it downloads the previous release into `./output/releases/<channel>` so Velopack can build the delta package and keep the full feed: without that step the feed would only list the new version.

Code signing is wired through the `VPK_*` environment variables Velopack reads natively and stays disabled while the corresponding repository secrets are empty. Unsigned Windows builds show the SmartScreen warning and unsigned macOS builds are blocked by Gatekeeper.

## Updates

The application checks the Velopack release feed at `https://misc.anti-soft.ru/catomania/<platform>`, where `<platform>` is `win`, `linux` or `osx`:

- Right-click the cat and choose **Check for Updates**.
- Or run `Catomania --update-check`, optionally with `--update-url <uri>` to use another feed. The command prints the check result and exits, applying the update when one is available.

Self-update works when the application is installed by Velopack: on Linux run the packed `catomania.AppImage` (the updater replaces the AppImage file in place), on Windows run the installed setup. Development and portable builds only report the available version.

## Configuration

Settings are stored as JSON in the user configuration directory:

| OS | Path |
| --- | --- |
| Linux | `~/.config/Catomania/settings.json` |
| Windows | `%APPDATA%\Catomania\settings.json` |
| macOS | `~/Library/Application Support/Catomania/settings.json` |

| Field | Values | Default | Description |
| --- | --- | --- | --- |
| `speed` | `Slow`, `Normal`, `Fast` | `Normal` | How fast the cat chases the cursor. |
| `colorScheme` | `Ginger`, `Gray`, `Black`, `Cream` | `Ginger` | Fur color. |
| `size` | `Small`, `Medium`, `Large` | `Medium` | Cat size. |
| `alwaysOnTop` | `true`, `false` | `true` | Keep the cat above other windows. |
| `paused` | `true`, `false` | `false` | The cat stays in place and does not chase. |
| `sleepAfterSeconds` | number | `30` | Cursor inactivity before the cat falls asleep. |

## How it works

- `Views/CatWindow` is a small borderless transparent topmost window that hosts the cat.
- `Services/CatChaseService` runs a ~60 FPS loop on the UI thread: it reads the cursor position, advances the simulation and moves the window.
- `Input/ICursorPositionProvider` reads the global cursor position: `XQueryPointer` on X11, `GetCursorPos` on Windows and `CGEventGetLocation` on macOS. When the platform API is not available, the position reported by the pointer events of the cat window itself is used.
- `Simulation/CatSimulator` is the pure movement and animation state machine (idle, walk, run, sleep, dragged), it does not know anything about windows and is covered by unit tests.
- `Controls/CatView` draws the cat in a 100x100 design space scaled to the window, animating legs, tail, ears, eyes and blinks.

## Limitations

- On a native Wayland session without XWayland the global cursor position is not available, so the cat follows the pointer only while it is over the cat window.
- The cat window is a small rectangle; clicks inside the box but outside the drawn cat are captured by the window.

## License

Catomania is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
