# Setup and build

## Open the final source

Use Unity **6000.3.4f1**. The version is recorded in
[ProjectVersion.txt](../../Project/04-Final/ProjectSettings/ProjectVersion.txt), with dependencies in
[manifest.json](../../Project/04-Final/Packages/manifest.json).

1. Download or clone this submission repository.
2. In Unity Hub, add `Project/04-Final/`, which is the Unity project directory.
3. Wait for import to finish.
4. Open `Assets/Scenes/Main.unity` and press Play.
5. Choose Fill or Endless. The menu enters `ConstructionPrototype.unity`.

For an individual source ZIP, extract it and add the folder containing `Assets`, `Packages` and `ProjectSettings`.
The historical tangible prototype uses its nested `unity/` folder instead. Each [version page](../../Project/README.md) gives the correct entry.

## Downloads and versions

The [final source](../../Project/04-Final/README.md) is the 22 November snapshot.
The Windows download is the 19 November executable. Extract its whole ZIP and keep the data, Mono and Unity runtime files beside the EXE.
The macOS download contains source rather than a built application. All entries are labelled in [downloads](../../Project/Downloads/README.md).

## Make a build

Keep Main and ConstructionPrototype in the build scene list. Choose `Urban Wildlife > Build Windows No-Camera` in the editor.
The batch entry is `UrbanWildlifeRooms.Editor.UrbanWildlifeBuildPipeline.BuildWindowsNoCamera`.
The default output is `Builds/Windows-NoCamera/` within the Unity project. `SYMBIOSIS49_BUILD_OUTPUT_ROOT` selects another output location.
The [build code](../../Project/04-Final/Assets/Scripts/Editor/UrbanWildlifeBuildPipeline.cs) records the platform-specific steps.

For macOS, use the matching Unity module and the `Build macOS No-Camera` menu or Build Profiles.
No built, signed or Mac-tested app is included in the current downloads.

## Supporting tools

Ordinary Fill and Endless play do not require Python or a camera.
The [camera recogniser](../../Project/04-Final/Tools/CameraRecognition/README.md) is a separate technical path.
The optional [leaderboard service](../../Project/04-Final/Services/leaderboard/README.md) has its own setup.

From the submission repository root, run `python Misc/tools/verify_archive.py` to check document links and source hashes.
The [saved software reports](../evidence/automated-tests/README.md) describe named snapshots and selected checks.
