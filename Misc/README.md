# Miscellaneous supporting material

Start with the [assessment reading guide](../Documentation/README.md) for the main project account.
This folder holds supporting detail and earlier material. Files are retained so a reader can trace a claim or rebuild the work.

| Folder | Contents | File types |
| --- | --- | --- |
| [technical](technical/README.md) | Setup, architecture, modes, gameplay rules, controls, saves and font notices | .md |
| [process](process/README.md) | Repository lineage, release notes and dated revision records | .md |
| [process/history](process/history/README.md) | Commit tables, recorded repository heads and file-move records | .csv, .json, .md |
| [evidence/automated-tests](evidence/automated-tests/README.md) | Saved software reports, calibration data and report indexes | .xml, .json, .md |
| [archive](archive/README.md) | Superseded design documents, old overviews and reference images | .md, .docx, .png |

## Project folders kept in place

The Unity project uses [Assets](../Assets/), [Packages](../Packages/) and [ProjectSettings](../ProjectSettings/).
They stay at the repository root so Unity can open the project.

[SourceAssets](../SourceAssets/) contains editable artwork and model records.
[Tools](../Tools/) contains modelling, camera and archive helpers.
The optional [leaderboard service](../Services/leaderboard/README.md) has its own setup.
These folders keep their paths so existing scripts and project references continue to work.

## Reading old records

Dates, results and superseded rules belong to the snapshot named in each record.
Saved test reports have not been rerun for this reorganisation. Use [project status](../Documentation/PROJECT_STATUS.md)
to compare source and download versions. Private participant material is not included.
