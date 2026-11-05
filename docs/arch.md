# Project Architecture

## Linux and Wayland

Wayland does not let applications read the global cursor position, and XWayland knows it only while the cursor is over an X11 window, that is, over the cat. So on Wayland Catomania asks the compositor:

- **KDE Plasma**: a small KWin script is loaded over D-Bus when the cat starts and unloaded when it quits. Nothing has to be installed.
- **GNOME**: the `catomania-cursor@catomania` GNOME Shell extension is installed into `~/.local/share/gnome-shell/extensions` and enabled on the first run. GNOME Shell on Wayland picks up new extensions only on login, so **log out and back in once** after the first run. If the cat still follows the cursor only nearby, check that the extension is enabled in the Extensions application. To remove it, run `gnome-extensions uninstall catomania-cursor@catomania`.

The extension and the script share only the cursor position. On other Wayland compositors the cat sees the cursor only when it is close by.

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
| `colorScheme` | `Ginger`, `Gray`, `Black`, `Cream` | `Ginger` | Cat fur color. |
| `dogCoat` | `Liver`, `Black`, `Red`, `Lemon` | `Liver` | Color of the dog's patches on the white coat. |
| `snakeSkin` | `Mint`, `DarkGreen`, `Blue`, `Pink`, `Yellow` | `Mint` | Snake skin color. |
| `parrotPlumage` | `Scarlet`, `BlueAndGold`, `Green`, `Hyacinth` | `Scarlet` | Parrot plumage, a macaw species. |
| `size` | `Small`, `Medium`, `Large` | `Medium` | Cat size. |
| `alwaysOnTop` | `true`, `false` | `true` | Keep the cat above other windows. |
| `paused` | `true`, `false` | `false` | The cat stays in place and does not chase. |
| `sleepAfterSeconds` | number | `30` | Cursor inactivity before the cat falls asleep. |

## Localization

The interface follows the operating system's current UI culture, detected from the environment at startup. English, Russian, Spanish, Japanese, Vietnamese, Chinese, French, Italian, and German are supported; other languages fall back to English. Translations are stored in `Resources/Strings*.resx`. Animal speech is not translated.

## How it works

- `Views/AnimalWindow` is a small borderless transparent topmost window that hosts the animal.
- `Services/AnimalChaseService` runs a ~60 FPS simulation loop on the UI thread: it reads the cursor position, advances the simulation and moves the window. Rendering stays at 60 FPS while the animal is awake and is throttled to 15 FPS when sleeping.
- `Input/ICursorPositionProvider` reads the global cursor position: `XQueryPointer` on X11, `GetCursorPos` on Windows and `CGEventGetLocation` on macOS. When the platform API is not available, the position reported by the pointer events of the animal window itself is used.
- `Simulation/AnimalSimulator` is the pure movement and animation state machine (idle, walk, run, sleep, dragged), it does not know anything about windows and is covered by unit tests. Each animal's `AnimalSimulatorOptions` can define multiple sleeping animation variants and its own pool of pause-mode tricks. The selected sleeping animation index and current trick are passed to the drawing code in `AnimalRenderState`. Besides the common tricks, an animal can add its own animal-specific tricks to that pool, for example `CapybaraSong`, which only the capybara has. Their durations come from `AnimalSimulatorOptions.TrickDurations`, and the window shows the text from `Animal.GetTrickSpeech` in the speech bubble, so the capybara sings a long "Ca-pi-baa-ra..." and four short "capibara", swinging its booty once per note.
- `Controls/AnimalView` draws the animal in a 100x100 design space scaled to the window, animating its body, face and other features.

## Limitations

- On a native Wayland session without XWayland the global cursor position is not available, so the cat follows the pointer only while it is over the cat window.
- The cat window is a small rectangle; clicks inside the box but outside the drawn cat are captured by the window.
