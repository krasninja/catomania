# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Initial implementation: a cat that chases the mouse cursor across the screen.
- Vector drawn cat with idle, walk, run, sleep and dragged animations, four fur colors and three sizes.
- Global cursor tracking on X11, Windows and macOS, with a window based fallback.
- Cursor tracking on Wayland: a KWin script on KDE Plasma and a GNOME Shell extension on GNOME.
- Context menu with pause, speed, fur, size, always on top, recenter and quit; settings are persisted as JSON.
- Cake Frosting build script (`./build.sh`) that publishes single file binaries and packs Velopack releases for Linux, Windows and macOS.
- Velopack self-updates: the context menu and the `--update-check` command line option check the release feed and update the installed application.
- `Animal` base class: the chase behavior, window and menus are shared by all animals, each animal draws itself and tunes its movement. The cat is the only animal so far.
- Cat gait: legs swing from the hip and lift the feet, the animation follows the distance travelled so the feet do not slide at any speed or size, dragged legs dangle, the tail sways in a wave and the sleeping cat breathes without sinking into the ground.
- Panda cub: a second animal with a big round head, stubby legs and a waddle, it dozes off sitting up. The "Animal" menu switches between the cat and the panda, the fur colors apply to the cat only.
- Dog: a liver and white springer spaniel drawn in profile with long legs, a bounding run with flying ears, it sleeps stretched out and paddles in its dreams.
