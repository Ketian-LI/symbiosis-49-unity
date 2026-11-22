# Setup and build

Use Unity **6000.3.4f1**. The editor version is in [ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt);
packages are listed in [manifest.json](../../Packages/manifest.json).

## Open the source

1. Download this branch or clone the repository.
2. Add the project root to Unity Hub and open it with the matching editor.
3. Wait for package and asset import to finish.
4. Open `Assets/Scenes/Main.unity` and press Play.
5. Choose Fill or Endless. The menu enters `ConstructionPrototype.unity`.

The [scene and mode guide](SCENES_AND_MODES.md) explains that transition and the separate BoardGame path.

The [project status](../../Documentation/PROJECT_STATUS.md) identifies this source and the older downloads.
The source contains interface changes made after the 19 November Windows build.
See [release notes](../process/RELEASE_NOTES.md) before comparing screenshots with a download.

## Run the Windows download

Extract the whole ZIP. Run `SYMBIOSIS49-NoCamera.exe` with its `_Data`, Mono and Unity runtime files beside it.
Do not move the executable out of that folder.

## Make a build

Check that Main and ConstructionPrototype are in the build scene list.
Choose `Urban Wildlife > Build Windows No-Camera` in the editor.
The batch entry is `UrbanWildlifeRooms.Editor.UrbanWildlifeBuildPipeline.BuildWindowsNoCamera`.

The default output is `Builds/Windows-NoCamera/`.
Set `SYMBIOSIS49_BUILD_OUTPUT_ROOT` to use another output folder.
The [build code](../../Assets/Scripts/Editor/UrbanWildlifeBuildPipeline.cs) gives the platform-specific steps.

For macOS, use a Mac with the matching Unity module. Choose the target architecture in Build Profiles,
or use the editor's `Build macOS No-Camera` menu. The published Mac download contains source only.
No built, signed or Mac-tested app is included.

## Optional services

Ordinary Fill and Endless play do not need a camera or Python.
The [camera recogniser](../../Tools/CameraRecognition/README.md) is a separate experimental path.
The [leaderboard service](../../Services/leaderboard/README.md) has its own setup and local checks.

## Check the documents and source

Run `python Tools/archive/verify_archive.py` from the repository root.
Use Unity Test Runner for gameplay checks. The [saved reports](../evidence/automated-tests/README.md) state what was checked;
they do not claim a fresh pass of every test.
