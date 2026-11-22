# Original-art camera bridge (staging)

This directory is a standalone prototype for the printed illustrated board.
The existing `symbiosis49_camera.py` still recognizes ArUco stickers and has
not been changed. `symbiosis49_art_camera.py` instead uses the four printed
corner motifs and the 80 approved room illustrations.

## Run a still frame

From this directory, with Python and OpenCV installed:

```powershell
python .\symbiosis49_art_camera.py --source .\pocket3-frame.png --no-send
python .\symbiosis49_art_camera.py --source .\pocket3-frame.png --classifier knn --no-send
```

To send snapshots to the running Unity camera mode, remove `--no-send`. The
packet uses the existing UDP endpoint `127.0.0.1:49549` and schema version 1.
The original art ID 34 is mapped to Unity marker ID 10 only when it is at the
center cell; every other art ID maps to a unique marker ID with the same room
category via `artwork/art_tile_mapping.json`.

For a connected camera, use `--source <camera-index> --preview`; `q` or Esc
ends the preview. This live path has **not** yet been validated with physical
multi-room arrangements.

The optional trained KNN model remains behind `--classifier knn`. Its
thresholds are similarity at least 0.76 and runner-up margin at least 0.12.
Both classifiers first check cell occupancy. When any occupied cell is
unrecognized, the bridge emits an error packet with no tile list. It leaves
`calibrated=true` when the board itself is found, so Unity can show the
specific occupied-cell error and cannot build from an incomplete observation.

## Verified on 2026-11-06

- One actual Pocket 3 still frame: all four printed motifs and 7x7 grid
  located; center art ID 34 maps to the Unity starter oak at column 3, row 3;
  48 cells remain empty.
- 5 local tests passed: real frame, optional KNN real frame, two-tile
  synthetic mapping, unrelated frame rejection, and unknown occupied-cell
  rejection for both classifiers.
- Warm-path static timing on this computer: one real tile ~30 ms for template
  matching or ~24 ms for KNN; a synthetic 49-textured-cell frame ~372 ms or
  ~54 ms respectively. Startup/model loading took about 1.9 seconds.

The 49-tile frame and changed lighting checks are synthetic. They do not prove
real multi-room or motion accuracy, camera calibration stability over time,
USB reconnection, or Unity gameplay acceptance. Those need physical tests.

## Reproduce tests and timings

```powershell
python -m unittest discover -s . -p test_art_camera.py -v
python .\benchmark_art_camera.py --classifier template
python .\benchmark_art_camera.py --classifier knn
```
