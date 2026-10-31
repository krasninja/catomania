# AGENTS.md

## Project overview

Catomania is a cross-platform Avalonia desktop application written in C#. It displays an animated animal that follows the mouse cursor. The solution requires .NET 10 and uses Native AOT and Velopack for release builds.

## Repository layout

- `src/Catomania/` — application code.
- `src/Catomania.Tests/` — unit tests.
- `src/Catomania.Build/` — Cake Frosting build and packaging targets.
- `docs/arch.md` — architecture, platform integration, and configuration details.
- `BUILD.md` — build, packaging, CI, and update instructions.
- `output/` — generated publish and release artifacts; do not edit manually.

## Common commands

Run commands from the repository root:

```bash
dotnet build ./src/Catomania.sln
dotnet test ./src/Catomania.Tests
dotnet run --project ./src/Catomania
```

Package a release on the matching operating system and architecture:

```bash
./build.sh --target=Publish-Linux
./build.sh --target=Publish-Windows
./build.sh --target=Publish-Mac
```

See `BUILD.md` for packaging prerequisites and options.

## Code conventions

- Follow the existing C# style and `.editorconfig` settings.
- Nullable reference types and implicit usings are enabled; StyleCop analyzers apply to all projects.
- Keep `Simulation/CatSimulator` independent of windowing and platform APIs. Add or update unit tests when changing simulation behavior.
- Keep platform-specific cursor and desktop integration behind the existing abstractions.
- Preserve Native AOT compatibility when adding dependencies or reflection-based code.
- Use the existing project dependencies and patterns rather than introducing new frameworks unnecessarily.

## Validation

Build the solution and run the tests after code changes. Exercise the affected application behavior when practical, especially for platform-specific input, windowing, packaging, or update changes. Do not treat a successful build alone as validation of those behaviors.

Update `README.md`, `BUILD.md`, or `docs/arch.md` when a change makes their user-facing or architectural documentation outdated.
