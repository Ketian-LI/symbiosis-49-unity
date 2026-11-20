"""OpenCV ArUco sidecar for the SYMBIOSIS: 49 construction prototype."""

from __future__ import annotations

import argparse
import csv
import json
import math
import socket
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import cv2
import numpy as np


SCHEMA_VERSION = 1
GRID_SIZE = 7
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 49549
DEFAULT_CONFIG = Path(__file__).with_name("marker_config.json")
FIRST_TILE_MARKER_ID = 10
LAST_TILE_MARKER_ID = 89
CAMERA_READ_FAILURES_BEFORE_REOPEN = 10
CAMERA_READ_RETRY_SECONDS = 0.05
CAMERA_REOPEN_RETRY_SECONDS = 0.5


@dataclass(frozen=True)
class TileDefinition:
    marker_id: int
    category: str
    planting: str
    residence_kind: str


@dataclass(frozen=True)
class DetectorConfig:
    dictionary_name: str
    corners: dict[str, int]
    tiles: dict[int, TileDefinition]
    cell_size_mm: float
    inter_cell_gap_mm: float
    camera_model: str
    initial_lens_height_mm: float
    provisional_corner_label_size_mm: float
    provisional_corner_code_size_mm: float
    provisional_tile_label_size_mm: float
    provisional_tile_code_size_mm: float
    boundary_margin: float

    @property
    def active_grid_size_mm(self) -> float:
        return GRID_SIZE * self.cell_size_mm + (GRID_SIZE - 1) * self.inter_cell_gap_mm


def load_config(path: Path) -> DetectorConfig:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != SCHEMA_VERSION:
        raise ValueError(f"Unsupported config schema: {data.get('schemaVersion')}")
    dictionary_name = data.get("dictionary", "")
    if not hasattr(cv2.aruco, dictionary_name):
        raise ValueError(f"OpenCV has no ArUco dictionary named {dictionary_name}")

    required_roles = {"topLeft", "topRight", "bottomRight", "bottomLeft"}
    corners = data.get("cornerMarkerIds") or {}
    if set(corners) != required_roles or len(set(corners.values())) != 4:
        raise ValueError("cornerMarkerIds must define four unique board corners")
    expected_corners = {"topLeft": 0, "topRight": 1, "bottomRight": 2, "bottomLeft": 3}
    if corners != expected_corners:
        raise ValueError("This MVP requires corner IDs 0, 1, 2, 3 in documented order")

    tiles: dict[int, TileDefinition] = {}
    for item in data.get("tiles") or []:
        marker_id = int(item["markerId"])
        if not FIRST_TILE_MARKER_ID <= marker_id <= LAST_TILE_MARKER_ID:
            raise ValueError(
                f"Tile marker ID must be in the reserved range "
                f"{FIRST_TILE_MARKER_ID}-{LAST_TILE_MARKER_ID}: {marker_id}"
            )
        if marker_id in tiles:
            raise ValueError(f"Duplicate tile marker definition: {marker_id}")
        category = str(item["category"])
        residence_kind = str(item.get(
            "residenceKind", "Standard" if category == "Residence" else "None"
        ))
        tiles[marker_id] = TileDefinition(
            marker_id=marker_id,
            category=category,
            planting=str(item.get("planting", "None")),
            residence_kind=residence_kind,
        )
    starter = tiles.get(10)
    if starter is None or starter.category != "Green" or starter.planting != "Oak":
        raise ValueError("Marker 10 must be configured as the starter Green/Oak tile")
    valid_categories = {
        "Green", "Residence", "Workshop", "Restaurant",
        "Supermarket", "Waste", "Street", "Square",
    }
    valid_plantings = {"None", "Oak", "Meadow", "Shrub"}
    # Homes share one physical tile type. Garden/urban bonuses are earned from
    # the completed board layout, not encoded into the printed marker.
    valid_residence_kinds = {"Standard"}
    for definition in tiles.values():
        if definition.category not in valid_categories:
            raise ValueError(
                f"Marker {definition.marker_id} has unknown category {definition.category}"
            )
        if definition.planting not in valid_plantings:
            raise ValueError(
                f"Marker {definition.marker_id} has unknown planting {definition.planting}"
            )
        if (definition.category != "Green" and definition.planting != "None") or (
            definition.category == "Green" and
            definition.marker_id != 10 and definition.planting != "None"
        ):
            raise ValueError(
                f"Marker {definition.marker_id} has an invalid category/planting pair; "
                "only starter marker 10 fixes Oak, while other green tiles "
                "are resolved by board position"
            )
        if definition.category == "Residence":
            if definition.residence_kind not in valid_residence_kinds:
                raise ValueError(
                    f"Marker {definition.marker_id} has invalid residence kind "
                    f"{definition.residence_kind}"
                )
        elif definition.residence_kind != "None":
            raise ValueError(
                f"Marker {definition.marker_id} cannot have residence kind "
                f"{definition.residence_kind}"
            )
    cell_size_mm = float(data.get("cellSizeMm", 0.0))
    if cell_size_mm <= 0.0:
        raise ValueError("cellSizeMm must be greater than 0")
    inter_cell_gap_mm = float(data.get("interCellGapMm", 0.0))
    if inter_cell_gap_mm < 0.0:
        raise ValueError("interCellGapMm must be at least 0")
    camera_model = str(data.get("cameraModel", "")).strip()
    if not camera_model:
        raise ValueError("cameraModel must not be empty")
    initial_lens_height_mm = float(data.get("initialLensHeightMm", 0.0))
    if initial_lens_height_mm <= 0.0:
        raise ValueError("initialLensHeightMm must be greater than 0")
    provisional_corner_label_size_mm = float(
        data.get("provisionalCornerLabelSizeMm", 0.0)
    )
    provisional_corner_code_size_mm = float(
        data.get("provisionalCornerCodeSizeMm", 0.0)
    )
    if provisional_corner_code_size_mm <= 0.0:
        raise ValueError("provisionalCornerCodeSizeMm must be greater than 0")
    if provisional_corner_label_size_mm <= provisional_corner_code_size_mm:
        raise ValueError(
            "provisionalCornerLabelSizeMm must be greater than the code size"
        )
    provisional_tile_label_size_mm = float(
        data.get("provisionalTileLabelSizeMm", 0.0)
    )
    provisional_tile_code_size_mm = float(
        data.get("provisionalTileCodeSizeMm", 0.0)
    )
    if provisional_tile_code_size_mm <= 0.0:
        raise ValueError("provisionalTileCodeSizeMm must be greater than 0")
    if provisional_tile_label_size_mm <= provisional_tile_code_size_mm:
        raise ValueError(
            "provisionalTileLabelSizeMm must be greater than the code size"
        )
    if provisional_tile_label_size_mm >= cell_size_mm:
        raise ValueError(
            "provisionalTileLabelSizeMm must be smaller than the physical cell"
        )
    margin = float(data.get("boundaryMargin", 0.12))
    if not 0.0 <= margin < 0.5:
        raise ValueError("boundaryMargin must be at least 0 and less than 0.5")
    return DetectorConfig(
        dictionary_name,
        corners,
        tiles,
        cell_size_mm,
        inter_cell_gap_mm,
        camera_model,
        initial_lens_height_mm,
        provisional_corner_label_size_mm,
        provisional_corner_code_size_mm,
        provisional_tile_label_size_mm,
        provisional_tile_code_size_mm,
        margin,
    )


def aruco_dictionary(config: DetectorConfig) -> Any:
    return cv2.aruco.getPredefinedDictionary(
        getattr(cv2.aruco, config.dictionary_name)
    )


def detect_packet(
    frame: np.ndarray, config: DetectorConfig, sequence: int
) -> tuple[dict[str, Any], np.ndarray]:
    if frame is None or frame.size == 0:
        return _packet(sequence, False, 0, [], "Camera frame is empty."), frame

    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) if frame.ndim == 3 else frame
    detector = cv2.aruco.ArucoDetector(
        aruco_dictionary(config), cv2.aruco.DetectorParameters()
    )
    marker_corners, marker_ids, _ = detector.detectMarkers(gray)
    annotated = frame.copy()
    if marker_ids is not None:
        cv2.aruco.drawDetectedMarkers(annotated, marker_corners, marker_ids)

    detected: dict[int, np.ndarray] = {}
    duplicate_ids: set[int] = set()
    if marker_ids is not None:
        for corners, marker_id in zip(marker_corners, marker_ids.flatten()):
            marker_id = int(marker_id)
            if marker_id in detected:
                duplicate_ids.add(marker_id)
            detected[marker_id] = np.asarray(corners, dtype=np.float32).reshape(4, 2)

    corner_count = sum(marker_id in detected for marker_id in config.corners.values())
    relevant_duplicates = duplicate_ids.intersection(
        set(config.corners.values()).union(config.tiles)
    )
    if relevant_duplicates:
        duplicate_text = ", ".join(str(value) for value in sorted(relevant_duplicates))
        return (
            _packet(
                sequence,
                False,
                corner_count,
                [],
                f"Duplicate physical marker IDs detected: {duplicate_text}.",
            ),
            annotated,
        )
    if corner_count != 4:
        return (
            _packet(
                sequence,
                False,
                corner_count,
                [],
                f"Need four corner markers; detected {corner_count}.",
            ),
            annotated,
        )

    # OpenCV corners are canonical TL, TR, BR, BL for an upright printed marker.
    # The four calibration markers must therefore all be mounted upright.
    source = np.float32(
        [
            detected[config.corners["topLeft"]][2],
            detected[config.corners["topRight"]][3],
            detected[config.corners["bottomRight"]][0],
            detected[config.corners["bottomLeft"]][1],
        ]
    )
    destination = np.float32(
        [[0.0, 0.0], [GRID_SIZE, 0.0], [GRID_SIZE, GRID_SIZE], [0.0, GRID_SIZE]]
    )
    if abs(cv2.contourArea(source)) < 1.0:
        return _packet(sequence, False, 4, [], "Calibration corners are degenerate."), annotated
    homography = cv2.getPerspectiveTransform(source, destination)
    if homography is None or not np.isfinite(homography).all():
        return _packet(sequence, False, 4, [], "Calibration homography failed."), annotated

    observations: list[dict[str, Any]] = []
    for marker_id, definition in sorted(config.tiles.items()):
        corners = detected.get(marker_id)
        if corners is None:
            continue
        center = corners.mean(axis=0).reshape(1, 1, 2)
        board_point = cv2.perspectiveTransform(center, homography).reshape(2)
        x, y = float(board_point[0]), float(board_point[1])
        if not _inside_cell(x, y, config.boundary_margin):
            continue
        column, row = math.floor(x), math.floor(y)
        observation = {
            "markerId": marker_id,
            "column": column,
            "row": row,
            "category": definition.category,
            "planting": definition.planting,
        }
        if definition.category == "Residence":
            observation["residenceKind"] = definition.residence_kind
        observations.append(observation)
        pixel_center = tuple(int(round(value)) for value in corners.mean(axis=0))
        cv2.putText(
            annotated,
            f"{marker_id}: C{column} R{row}",
            pixel_center,
            cv2.FONT_HERSHEY_SIMPLEX,
            0.55,
            (30, 90, 220),
            2,
            cv2.LINE_AA,
        )

    return _packet(sequence, True, 4, observations, ""), annotated


def _inside_cell(x: float, y: float, boundary_margin: float) -> bool:
    if not (0.0 <= x < GRID_SIZE and 0.0 <= y < GRID_SIZE):
        return False
    for coordinate in (x, y):
        nearest = round(coordinate)
        if 0 < nearest < GRID_SIZE and abs(coordinate - nearest) < boundary_margin:
            return False
    return True


def _packet(
    sequence: int,
    calibrated: bool,
    corner_count: int,
    tiles: list[dict[str, Any]],
    error: str,
) -> dict[str, Any]:
    return {
        "schemaVersion": SCHEMA_VERSION,
        "sequence": sequence,
        "calibrated": calibrated,
        "cornerCount": corner_count,
        "error": error,
        "tiles": tiles,
    }


def generate_markers(output_dir: Path, config: DetectorConfig, pixels: int) -> None:
    if pixels < 16:
        raise ValueError("marker-pixels must be at least 16")
    output_dir.mkdir(parents=True, exist_ok=True)
    dictionary = aruco_dictionary(config)
    for marker_id in range(LAST_TILE_MARKER_ID + 1):
        image = cv2.aruco.generateImageMarker(dictionary, marker_id, pixels)
        target = output_dir / f"aruco_4x4_100_id_{marker_id:02d}.png"
        write_image(target, image)


def _generate_label_svg(
    output_dir: Path,
    config: DetectorConfig,
    marker_id: int,
    label_size: float,
    code_size: float,
    filename: str,
) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    dictionary = aruco_dictionary(config)
    module_count = 6  # DICT_4X4 marker plus one black border module per side.
    quiet_margin = (label_size - code_size) / 2.0
    module_size = code_size / module_count
    marker = cv2.aruco.generateImageMarker(dictionary, marker_id, module_count)
    black_rectangles: list[str] = []
    for row in range(module_count):
        for column in range(module_count):
            if int(marker[row, column]) < 128:
                x = quiet_margin + column * module_size
                y = quiet_margin + row * module_size
                black_rectangles.append(
                    f'  <rect x="{x:g}" y="{y:g}" width="{module_size:g}" '
                    f'height="{module_size:g}" fill="#000"/>'
                )
    svg = "\n".join(
        [
            '<?xml version="1.0" encoding="UTF-8"?>',
            (
                f'<svg xmlns="http://www.w3.org/2000/svg" '
                f'width="{label_size:g}mm" height="{label_size:g}mm" '
                f'viewBox="0 0 {label_size:g} {label_size:g}" '
                'shape-rendering="crispEdges">'
            ),
            f'  <title>ArUco DICT_4X4_100 ID {marker_id}</title>',
            f'  <rect width="{label_size:g}" height="{label_size:g}" fill="#fff"/>',
            *black_rectangles,
            "</svg>",
            "",
        ]
    )
    (output_dir / filename).write_text(svg, encoding="utf-8")


def generate_corner_label_svgs(output_dir: Path, config: DetectorConfig) -> None:
    for marker_id in range(4):
        _generate_label_svg(
            output_dir,
            config,
            marker_id,
            config.provisional_corner_label_size_mm,
            config.provisional_corner_code_size_mm,
            f"corner_id_{marker_id}_{config.provisional_corner_label_size_mm:g}mm.svg",
        )


def generate_tile_label_svgs(output_dir: Path, config: DetectorConfig) -> None:
    for marker_id, definition in sorted(config.tiles.items()):
        semantic_name = f"{definition.category}_{definition.planting}"
        if definition.category == "Residence":
            semantic_name += f"_{definition.residence_kind}"
        _generate_label_svg(
            output_dir,
            config,
            marker_id,
            config.provisional_tile_label_size_mm,
            config.provisional_tile_code_size_mm,
            (
                f"tile_id_{marker_id}_{semantic_name}_"
                f"{config.provisional_tile_label_size_mm:g}mm.svg"
            ),
        )


def generate_tile_sheet_svgs(output_dir: Path, config: DetectorConfig) -> None:
    """Create A4 sheets at actual size; keep the 32 mm marker geometry intact."""
    output_dir.mkdir(parents=True, exist_ok=True)
    dictionary = aruco_dictionary(config)
    label_size = config.provisional_tile_label_size_mm
    code_size = config.provisional_tile_code_size_mm
    if label_size > 32.0:
        raise ValueError("A4 tile sheets support labels up to 32 mm")
    margin = (label_size - code_size) / 2.0
    module_size = code_size / 6.0
    tiles = sorted(config.tiles.items())
    page_size = 35
    for page_index, first in enumerate(range(0, len(tiles), page_size), start=1):
        svg = [
            '<?xml version="1.0" encoding="UTF-8"?>',
            '<svg xmlns="http://www.w3.org/2000/svg" width="210mm" '
            'height="297mm" viewBox="0 0 210 297" shape-rendering="crispEdges">',
            '<rect width="210" height="297" fill="#fff"/>',
            f'<text x="15" y="12" font-family="Arial" font-size="4">'
            f'SYMBIOSIS: 49 · board markers · sheet {page_index}</text>',
        ]
        for slot, (marker_id, definition) in enumerate(
            tiles[first:first + page_size]
        ):
            x = 15 + (slot % 5) * 36
            y = 20 + (slot // 5) * 36
            svg.append(f'<g transform="translate({x:g},{y:g})">')
            svg.append(
                f'<rect width="{label_size:g}" height="{label_size:g}" '
                'fill="#fff" stroke="#aaa" stroke-width="0.15"/>'
            )
            marker = cv2.aruco.generateImageMarker(dictionary, marker_id, 6)
            for row in range(6):
                for column in range(6):
                    if int(marker[row, column]) < 128:
                        svg.append(
                            f'<rect x="{margin + column * module_size:g}" '
                            f'y="{margin + row * module_size:g}" '
                            f'width="{module_size:g}" height="{module_size:g}" '
                            'fill="#000"/>'
                        )
            kind = (("Oak" if marker_id == 10 else "Green")
                    if definition.category == "Green" else
                    definition.category)
            svg.append(
                f'<text x="0" y="35" font-family="Arial" font-size="2.5">'
                f'{marker_id} {kind}</text>'
            )
            svg.append('</g>')
        svg.extend([
            '<text x="15" y="287" font-family="Arial" font-size="2.7">'
            'Print at actual size / 100%; disable fit to page. '
            'Measure one 32 mm square before use.</text>',
            '</svg>',
            '',
        ])
        (output_dir / f'tile_markers_A4_{page_index:02d}.svg').write_text(
            '\n'.join(svg), encoding='utf-8'
        )


def generate_inventory_csv(output_path: Path, config: DetectorConfig) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open('w', encoding='utf-8-sig', newline='') as stream:
        writer = csv.writer(stream)
        writer.writerow(['markerId', 'category', 'planting', 'residenceKind', 'role'])
        for marker_id, tile in sorted(config.tiles.items()):
            writer.writerow([
                marker_id, tile.category, tile.planting, tile.residence_kind,
                'starter' if marker_id == 10 else '',
            ])


def read_image(path: Path) -> np.ndarray | None:
    # cv2.imread/imwrite still mishandle some Unicode Windows paths.
    try:
        encoded = np.fromfile(path, dtype=np.uint8)
    except OSError:
        return None
    return cv2.imdecode(encoded, cv2.IMREAD_COLOR) if encoded.size else None


def write_image(path: Path, image: np.ndarray) -> None:
    success, encoded = cv2.imencode(path.suffix or ".png", image)
    if not success:
        raise OSError(f"Could not encode {path}")
    try:
        encoded.tofile(path)
    except OSError as exception:
        raise OSError(f"Could not write {path}: {exception}") from exception


def parse_source(value: str) -> int | str:
    return int(value) if value.isdecimal() else value


def probe_cameras(
    max_index: int, width: int, height: int, fps: float
) -> list[dict[str, Any]]:
    if max_index < 0:
        raise ValueError("max-camera-index must be at least 0")
    available: list[dict[str, Any]] = []
    for index in range(max_index + 1):
        capture = cv2.VideoCapture(index)
        try:
            if not capture.isOpened():
                continue
            capture.set(cv2.CAP_PROP_FRAME_WIDTH, width)
            capture.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
            capture.set(cv2.CAP_PROP_FPS, fps)
            ok, frame = capture.read()
            if ok and frame is not None and frame.size:
                actual_height, actual_width = frame.shape[:2]
            else:
                actual_width = int(round(capture.get(cv2.CAP_PROP_FRAME_WIDTH)))
                actual_height = int(round(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)))
            available.append(
                {
                    "index": index,
                    "frameRead": bool(ok),
                    "width": int(actual_width),
                    "height": int(actual_height),
                    "reportedFps": float(capture.get(cv2.CAP_PROP_FPS)),
                }
            )
        finally:
            capture.release()
    return available


def send_packet(sender: socket.socket, host: str, port: int, packet: dict[str, Any]) -> None:
    payload = json.dumps(packet, separators=(",", ":")).encode("utf-8")
    sender.sendto(payload, (host, port))


def run(args: argparse.Namespace, config: DetectorConfig) -> int:
    print(
        f"Physical setup: {config.active_grid_size_mm:g} mm square active grid; "
        f"{config.camera_model}; initial lens height "
        f"{config.initial_lens_height_mm:g} mm (not hardware-verified)",
        flush=True,
    )
    if args.probe_cameras:
        available = probe_cameras(
            args.max_camera_index, args.width, args.height, args.fps
        )
        print(json.dumps({"availableCameras": available}, indent=2), flush=True)
        if not available:
            print(
                "No OpenCV camera index opened. Confirm that the camera is connected "
                "and not held by another application.",
                file=sys.stderr,
            )
            return 2
        return 0
    generated_assets = False
    if args.generate_markers:
        generate_markers(args.generate_markers, config, args.marker_pixels)
        print(f"Generated marker IDs 0-{LAST_TILE_MARKER_ID} "
              f"in {args.generate_markers}")
        generated_assets = True
    if args.generate_corner_labels:
        generate_corner_label_svgs(args.generate_corner_labels, config)
        print(
            "Generated provisional corner-label SVGs for IDs 0-3 in "
            f"{args.generate_corner_labels}"
        )
        generated_assets = True
    if args.generate_tile_labels:
        generate_tile_label_svgs(args.generate_tile_labels, config)
        print(
            "Generated provisional tile-label SVGs for configured IDs in "
            f"{args.generate_tile_labels}"
        )
        generated_assets = True
    if args.generate_tile_sheets:
        generate_tile_sheet_svgs(args.generate_tile_sheets, config)
        print(f"Generated A4 tile-marker sheets in {args.generate_tile_sheets}")
        generated_assets = True
    if args.generate_inventory:
        generate_inventory_csv(args.generate_inventory, config)
        print(f"Generated physical tile inventory in {args.generate_inventory}")
        generated_assets = True
    if generated_assets:
        return 0

    source = parse_source(args.source)
    source_path = Path(source) if isinstance(source, str) else None
    image_suffixes = {".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"}
    sender = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    if source_path is not None and source_path.suffix.lower() in image_suffixes:
        frame = read_image(source_path)
        if frame is None:
            raise OSError(f"Could not read image: {source_path}")
        packet, annotated = detect_packet(frame, config, 1)
        print(json.dumps(packet, ensure_ascii=False, indent=2))
        if not args.no_send:
            send_packet(sender, DEFAULT_HOST, args.port, packet)
        if args.preview:
            cv2.imshow("SYMBIOSIS: 49 camera recognition", annotated)
            cv2.waitKey(0)
        sender.close()
        return 0 if packet["calibrated"] else 2

    capture = cv2.VideoCapture(source)
    if not capture.isOpened():
        capture.release()
        raise OSError(f"Could not open camera or video source: {args.source}")
    if isinstance(source, int):
        capture.set(cv2.CAP_PROP_FRAME_WIDTH, args.width)
        capture.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)
        capture.set(cv2.CAP_PROP_FPS, args.fps)
        actual_width = int(round(capture.get(cv2.CAP_PROP_FRAME_WIDTH)))
        actual_height = int(round(capture.get(cv2.CAP_PROP_FRAME_HEIGHT)))
        actual_fps = capture.get(cv2.CAP_PROP_FPS)
        print(
            f"Camera {source}: requested {args.width}x{args.height} @ {args.fps:g} FPS; "
            f"reported {actual_width}x{actual_height} @ {actual_fps:g} FPS",
            flush=True,
        )
    sequence = 0
    consecutive_read_failures = 0
    try:
        while True:
            if capture is None:
                # A disconnected USB camera may come back under the same
                # index. Retry at a bounded pace instead of polling a dead
                # capture object forever.
                time.sleep(CAMERA_REOPEN_RETRY_SECONDS)
                reopened = cv2.VideoCapture(source)
                if not reopened.isOpened():
                    reopened.release()
                    continue
                capture = reopened
                capture.set(cv2.CAP_PROP_FRAME_WIDTH, args.width)
                capture.set(cv2.CAP_PROP_FRAME_HEIGHT, args.height)
                capture.set(cv2.CAP_PROP_FPS, args.fps)
                consecutive_read_failures = 0
                print(f"Camera {source} reconnected.", flush=True)
                continue
            try:
                ok, frame = capture.read()
            except (OSError, cv2.error):
                ok, frame = False, None
            if not ok or frame is None or frame.size == 0:
                if source_path is not None:
                    break
                consecutive_read_failures += 1
                if consecutive_read_failures >= CAMERA_READ_FAILURES_BEFORE_REOPEN:
                    print(
                        f"Camera {source} stopped delivering frames; "
                        "waiting to reconnect.",
                        file=sys.stderr,
                        flush=True,
                    )
                    capture.release()
                    capture = None
                    consecutive_read_failures = 0
                time.sleep(CAMERA_READ_RETRY_SECONDS)
                continue
            consecutive_read_failures = 0
            sequence += 1
            packet, annotated = detect_packet(frame, config, sequence)
            if not args.no_send:
                send_packet(sender, DEFAULT_HOST, args.port, packet)
            if sequence % 30 == 1:
                print(json.dumps(packet, ensure_ascii=False), flush=True)
            if args.preview:
                cv2.imshow("SYMBIOSIS: 49 camera recognition", annotated)
                if cv2.waitKey(1) & 0xFF in (27, ord("q")):
                    break
    finally:
        if capture is not None:
            capture.release()
        sender.close()
        if args.preview:
            cv2.destroyAllWindows()
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    parser.add_argument("--source", default="0", help="Camera index, video, or still image")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--width", type=int, default=1280)
    parser.add_argument("--height", type=int, default=720)
    parser.add_argument("--fps", type=float, default=30.0)
    parser.add_argument("--probe-cameras", action="store_true")
    parser.add_argument("--max-camera-index", type=int, default=5)
    parser.add_argument("--preview", action="store_true")
    parser.add_argument("--no-send", action="store_true")
    parser.add_argument("--generate-markers", type=Path)
    parser.add_argument("--generate-corner-labels", type=Path)
    parser.add_argument("--generate-tile-labels", type=Path)
    parser.add_argument("--generate-tile-sheets", type=Path)
    parser.add_argument("--generate-inventory", type=Path)
    parser.add_argument("--marker-pixels", type=int, default=600)
    return parser


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    args = build_parser().parse_args()
    try:
        config = load_config(args.config)
        return run(args, config)
    except (OSError, ValueError, cv2.error) as exception:
        print(f"error: {exception}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
