# Amarion Codex

An in-game bestiary/knowledge database mod for [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/). Tracks enemies and NPCs the player has encountered through combat, targeting, or interaction. Undiscovered entries show as question marks. Content is organized by game zone.

## Features

- Automatically tracks NPCs you target, consider, or kill
- Browse discovered entries organized by zone/dungeon
- View NPC details: level, faction, loot tables, quest associations
- Undiscovered entries shown as `???` placeholders with discovery count per zone
- Open with keybind (default: K) or chat commands (`/codex`, `/bestiary`)
- Search across all discovered entries
- Per-character save data, persisted alongside game saves

## Requirements

- [Erenshor](https://store.steampowered.com/app/2382520/Erenshor/) (or Erenshor Playtest)
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) installed into your Erenshor directory

## Installation

1. Install BepInEx 5.4.x into your Erenshor game directory if you haven't already
2. Download `AmarionCodex.dll` from the [Releases](../../releases) page
3. Copy it to `<Erenshor>/BepInEx/plugins/AmarionCodex/`
4. Launch the game

## Building from Source

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (with .NET Framework 4.7.2 targeting pack)
- Erenshor installed (the build references game DLLs directly)

### Build

The project auto-detects your Erenshor install if it's in the default Steam location. Otherwise, pass the path explicitly:

```bash
dotnet build AmarionCodex/AmarionCodex.csproj -p:ErenshorDir="C:\path\to\Erenshor"
```

Or set the `ERENSHOR_DIR` environment variable:

```bash
set ERENSHOR_DIR=C:\path\to\Erenshor
dotnet build AmarionCodex/AmarionCodex.csproj
```

The build defaults to Release configuration. Output goes to `AmarionCodex/bin/Release/net472/AmarionCodex.dll`.

If Erenshor is detected, the DLL is also auto-deployed to `<Erenshor>/BepInEx/plugins/AmarionCodex/`.

### Auto-detection paths

The build checks these locations in order:

1. `-p:ErenshorDir=` parameter
2. `ERENSHOR_DIR` environment variable
3. `C:\Program Files (x86)\Steam\steamapps\common\Erenshor Playtest`
4. `C:\Program Files (x86)\Steam\steamapps\common\Erenshor`

## Antivirus Note

Windows Defender may flag this DLL as a false positive. This is common for BepInEx/Harmony mods because runtime method patching uses techniques that resemble code injection to heuristic scanners. The mod contains no malicious code — you can review the full source in the `AmarionCodex/` directory.

If flagged, add an exclusion for your `BepInEx/plugins/` folder in Windows Security settings.

## License

[MIT](LICENSE)
