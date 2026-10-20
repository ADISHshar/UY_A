# Treasure Hunt

A Unity third-person maze game where a robot explorer collects colored keys, unlocks matching doors, and reaches a hidden treasure vault while avoiding sentinels and traps.

## Requirements

- Unity **6000.6.1f1**, as specified in `ProjectSettings/ProjectVersion.txt`.
- Unity Hub and the build support module for your intended platform.
- Keyboard and mouse for gameplay.

Unity restores the project dependencies from `Packages/manifest.json` when you open it. Key packages include Universal Render Pipeline (17.6.0), Input System (1.20.0), and AI Navigation (2.0.14).

## Getting started

1. Clone or download this repository.
2. In Unity Hub, add the repository root as a project and open it with the required Unity version.
3. Allow Unity to import the assets and resolve packages.
4. Open `Assets/Scenes/TreasureHunt.unity`.
5. Enter Play mode and start the game from the main menu.

## How to play

Collect the colored keys and approach their matching doors to unlock them automatically. Open every door, then reach the treasure to win. Keys remain in your inventory after use.

Avoid patrolling sentinels, spikes, lasers, and collapsing floor tiles. Sprinting makes noise that nearby sentinels can investigate. Losing all lives ends the run. Successful runs record a personal best time locally for the maze seed.

| Input | Action |
| --- | --- |
| WASD or arrow keys | Move relative to the camera |
| Mouse | Rotate the third-person camera |
| Left Shift while moving | Sprint |
| Escape | Pause |
| Menu buttons | Start, resume, restart, or quit |

## Maze customization

Open **Treasure Hunt > Maze Designer** in the Unity Editor. The settings asset is located at `Assets/TreasureHunt/Resources/MazeSettings.asset`.

You can configure the maze dimensions, seed, cell size, lives, guards, doors, and hazard counts and timing. The same seed and layout settings reproduce the same maze.

Outside Play mode, select **GENERATE COMPLETE EDITABLE LEVEL** to apply changes, then save the scene. Regeneration replaces the maze and guards while preserving existing UI and lighting, so save any manual level edits you want to retain before regenerating.

## Project layout

| Path | Contents |
| --- | --- |
| `Assets/Scenes/TreasureHunt.unity` | Main game scene; enabled in the build scene list |
| `Assets/TreasureHunt/Scripts/` | Game state, player, camera, maze, guards, hazards, UI, and audio |
| `Assets/TreasureHunt/Editor/` | Maze designer and scene setup/review tools |
| `Assets/TreasureHunt/Resources/` | Maze settings and audio bank assets |
| `Assets/TreasureHunt/Generated/` | Generated game assets |
| `Assets/Settings/` | Rendering and pipeline settings |
| `Packages/` | Dependency manifest and lock file |
| `ProjectSettings/` | Unity project configuration |

## Building

1. Open **File > Build Profiles** in Unity.
2. Select your target platform and install its build support module if needed.
3. Confirm that `Assets/Scenes/TreasureHunt.unity` is enabled in the scene list.
4. Build to a dedicated output folder, such as `Builds/`.

## Gameplay verification

The project includes a runtime verification routine in `HuntVerification.cs`. Launch a built player with the `-huntSmokeTest` argument to invoke it. Inspect the player log for `HUNT CHECK PASSED` and `HUNT CHECK FAILED` messages. The routine also writes review screenshots beside the build's data folder.
