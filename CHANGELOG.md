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
