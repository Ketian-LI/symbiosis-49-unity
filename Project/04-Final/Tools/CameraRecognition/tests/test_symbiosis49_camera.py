import json
import sys
import tempfile
import unittest
from collections import Counter
from pathlib import Path
from unittest.mock import patch

import cv2
import numpy as np


TOOL_DIR = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(TOOL_DIR))

from symbiosis49_camera import (  # noqa: E402
    CAMERA_READ_FAILURES_BEFORE_REOPEN,
    aruco_dictionary,
    build_parser,
    detect_packet,
    generate_corner_label_svgs,
    generate_inventory_csv,
    generate_markers,
    generate_tile_label_svgs,
    generate_tile_sheet_svgs,
    load_config,
    probe_cameras,
    run,
)


class SyntheticBoardTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.config = load_config(TOOL_DIR / "marker_config.json")
        cls.dictionary = aruco_dictionary(cls.config)

    def test_confirmed_physical_setup_is_loaded(self):
        self.assertEqual(self.config.cell_size_mm, 43.0)
        self.assertEqual(self.config.inter_cell_gap_mm, 0.0)
        self.assertEqual(self.config.active_grid_size_mm, 301.0)
        self.assertEqual(self.config.camera_model, "DJI Osmo Pocket 3")
        self.assertEqual(self.config.initial_lens_height_mm, 450.0)
        self.assertEqual(self.config.provisional_corner_label_size_mm, 20.0)
        self.assertEqual(self.config.provisional_corner_code_size_mm, 15.0)
        self.assertEqual(self.config.provisional_tile_label_size_mm, 32.0)
        self.assertEqual(self.config.provisional_tile_code_size_mm, 24.0)

    def test_full_physical_inventory_has_80_unique_mapped_tiles(self):
        self.assertEqual(sorted(self.config.tiles), list(range(10, 90)))
        counts = Counter(tile.category for tile in self.config.tiles.values())
        self.assertEqual(counts, {
            "Green": 21, "Residence": 12, "Workshop": 7,
            "Restaurant": 5, "Supermarket": 5, "Waste": 7,
            "Street": 12, "Square": 11,
        })
        self.assertEqual(Counter(
            tile.planting for tile in self.config.tiles.values()
            if tile.category == "Green"
        ), {"Oak": 1, "None": 20})
        self.assertEqual(Counter(
            tile.residence_kind for tile in self.config.tiles.values()
            if tile.category == "Residence"
        ), {"Standard": 12})

    def test_raw_marker_generator_covers_all_reserved_ids(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            generate_markers(output_dir, self.config, 24)
            self.assertEqual(len(list(output_dir.glob('aruco_4x4_100_id_*.png'))), 90)
            self.assertTrue((output_dir / 'aruco_4x4_100_id_89.png').exists())

    def test_printable_corner_svg_dimensions_and_ids(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)

            generate_corner_label_svgs(output_dir, self.config)

            generated = sorted(output_dir.glob("corner_id_*.svg"))
            self.assertEqual(len(generated), 4)
            for marker_id, path in enumerate(generated):
                svg = path.read_text(encoding="utf-8")
                self.assertIn('width="20mm" height="20mm"', svg)
                self.assertIn(f"DICT_4X4_100 ID {marker_id}", svg)
                self.assertIn('x="2.5" y="2.5"', svg)

    def test_printable_tile_svg_dimensions_and_mapped_ids(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)

            generate_tile_label_svgs(output_dir, self.config)

            generated = sorted(output_dir.glob("tile_id_*.svg"))
            self.assertEqual(len(generated), 80)
            self.assertEqual(
                [int(path.name.split("_")[2]) for path in generated],
                list(range(10, 90)),
            )
            for path in generated:
                svg = path.read_text(encoding="utf-8")
                self.assertIn('width="32mm" height="32mm"', svg)
                self.assertIn('x="4" y="4"', svg)

    def test_a4_sheets_keep_all_80_printable_labels_at_32_mm(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            generate_tile_sheet_svgs(output_dir, self.config)
            sheets = sorted(output_dir.glob("tile_markers_A4_*.svg"))
            self.assertEqual(len(sheets), 3)
            self.assertEqual(
                [sheet.read_text(encoding="utf-8").count('<g transform=')
                 for sheet in sheets], [35, 35, 10]
            )
            for sheet in sheets:
                svg = sheet.read_text(encoding="utf-8")
                self.assertIn('width="210mm" height="297mm"', svg)
                self.assertIn('Print at actual size / 100%', svg)
            first_sheet = sheets[0].read_text(encoding="utf-8")
            self.assertIn('>10 Oak</text>', first_sheet)
            self.assertIn('>11 Green</text>', first_sheet)
            self.assertIn('>32 Residence</text>', first_sheet)
            self.assertNotIn('>32 Garden</text>', first_sheet)
            self.assertNotIn('>11 None</text>', first_sheet)

    def test_physical_inventory_csv_includes_every_marker(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            path = Path(temporary_directory) / 'inventory.csv'
            generate_inventory_csv(path, self.config)
            rows = path.read_text(encoding='utf-8-sig').splitlines()
            self.assertEqual(len(rows), 81)
            self.assertIn('10,Green,Oak,None,starter', rows[1])
            self.assertIn('32,Residence,None,Standard,', rows[23])
            self.assertIn('89,Square,None,None,', rows[80])

    def test_camera_probe_reports_only_open_indices_and_releases_all(self):
        class FakeCapture:
            instances = []

            def __init__(self, index):
                self.index = index
                self.released = False
                self.__class__.instances.append(self)

            def isOpened(self):
                return self.index in (1, 2)

            def set(self, _property, _value):
                return True

            def read(self):
                if self.index == 1:
                    return True, np.zeros((720, 1280, 3), dtype=np.uint8)
                return False, None

            def get(self, property_id):
                values = {
                    cv2.CAP_PROP_FRAME_WIDTH: 640.0,
                    cv2.CAP_PROP_FRAME_HEIGHT: 480.0,
                    cv2.CAP_PROP_FPS: 30.0,
                }
                return values.get(property_id, 0.0)

            def release(self):
                self.released = True

        with patch("symbiosis49_camera.cv2.VideoCapture", FakeCapture):
            cameras = probe_cameras(2, 1280, 720, 30.0)

        self.assertEqual([camera["index"] for camera in cameras], [1, 2])
        self.assertEqual((cameras[0]["width"], cameras[0]["height"]), (1280, 720))
        self.assertTrue(cameras[0]["frameRead"])
        self.assertEqual((cameras[1]["width"], cameras[1]["height"]), (640, 480))
        self.assertFalse(cameras[1]["frameRead"])
        self.assertTrue(all(instance.released for instance in FakeCapture.instances))

    def test_live_camera_reopens_after_repeated_failed_reads(self):
        captures = []
        sleeps = []

        class FakeCapture:
            def __init__(self, attempt):
                self.attempt = attempt
                self.read_count = 0
                self.released = False
                self.settings = []

            def isOpened(self):
                return self.attempt != 1  # First reconnect attempt still fails.

            def set(self, property_id, value):
                self.settings.append((property_id, value))
                return True

            def get(self, property_id):
                return {
                    cv2.CAP_PROP_FRAME_WIDTH: 1280.0,
                    cv2.CAP_PROP_FRAME_HEIGHT: 720.0,
                    cv2.CAP_PROP_FPS: 30.0,
                }.get(property_id, 0.0)

            def read(self):
                self.read_count += 1
                if self.attempt == 0:
                    return False, None
                if self.read_count == 1:
                    return True, np.zeros((2, 2, 3), dtype=np.uint8)
                raise KeyboardInterrupt  # End the otherwise persistent live loop.

            def release(self):
                self.released = True

        class FakeSocket:
            closed = False

            def close(self):
                self.closed = True

        def open_capture(_source):
            capture = FakeCapture(len(captures))
            captures.append(capture)
            return capture

        fake_socket = FakeSocket()
        packet = {"schemaVersion": 1, "sequence": 1, "calibrated": True,
                  "cornerCount": 4, "error": "", "tiles": []}
        args = build_parser().parse_args(["--source", "1", "--no-send"])
        with patch("symbiosis49_camera.cv2.VideoCapture", side_effect=open_capture), \
             patch("symbiosis49_camera.socket.socket", return_value=fake_socket), \
             patch("symbiosis49_camera.time.sleep", side_effect=sleeps.append), \
             patch("symbiosis49_camera.detect_packet", return_value=(packet, None)) as detect:
            with self.assertRaises(KeyboardInterrupt):
                run(args, self.config)

        self.assertEqual(captures[0].read_count,
                         CAMERA_READ_FAILURES_BEFORE_REOPEN)
        self.assertEqual(len(captures), 3)
        self.assertTrue(all(capture.released for capture in captures))
        self.assertGreaterEqual(sleeps.count(0.5), 2)
        self.assertEqual(detect.call_args.args[2], 1)
        self.assertEqual(len(captures[2].settings), 3,
                         "Reopened camera must restore requested capture settings.")
        self.assertTrue(fake_socket.closed)

    def test_video_file_eof_does_not_reopen_camera(self):
        captures = []

        class FakeCapture:
            def isOpened(self):
                return True

            def read(self):
                return False, None

            def release(self):
                pass

        class FakeSocket:
            def close(self):
                pass

        def open_capture(_source):
            captures.append(FakeCapture())
            return captures[-1]

        args = build_parser().parse_args(["--source", "clip.mp4", "--no-send"])
        with patch("symbiosis49_camera.cv2.VideoCapture", side_effect=open_capture), \
             patch("symbiosis49_camera.socket.socket", return_value=FakeSocket()), \
             patch("symbiosis49_camera.time.sleep") as sleep:
            result = run(args, self.config)

        self.assertEqual(result, 0)
        self.assertEqual(len(captures), 1)
        sleep.assert_not_called()

    def test_starter_marker_maps_to_central_cell(self):
        image = self.board_image([(10, 3, 3)])

        packet, _ = detect_packet(image, self.config, sequence=1)

        self.assertTrue(packet["calibrated"], packet["error"])
        self.assertEqual(packet["cornerCount"], 4)
        self.assertEqual(
            packet["tiles"],
            [
                {
                    "markerId": 10,
                    "column": 3,
                    "row": 3,
                    "category": "Green",
                    "planting": "Oak",
                }
            ],
        )

    def test_second_marker_maps_to_requested_cell(self):
        image = self.board_image([(10, 3, 3), (12, 3, 2)])

        packet, _ = detect_packet(image, self.config, sequence=2)

        self.assertTrue(packet["calibrated"], packet["error"])
        by_id = {item["markerId"]: item for item in packet["tiles"]}
        self.assertEqual((by_id[12]["column"], by_id[12]["row"]), (3, 2))
        self.assertEqual((by_id[12]["category"], by_id[12]["planting"]),
                         ("Green", "None"))

    def test_nonstarter_green_cannot_preassign_planting(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            config = json.loads((TOOL_DIR / 'marker_config.json').read_text(
                encoding='utf-8'))
            config['tiles'][1]['planting'] = 'Oak'
            path = Path(temporary_directory) / 'marker_config.json'
            path.write_text(json.dumps(config), encoding='utf-8')
            with self.assertRaisesRegex(ValueError,
                                        'only starter marker 10 fixes Oak'):
                load_config(path)

    def test_every_residence_marker_transmits_the_same_kind(self):
        image = self.board_image([
            (10, 3, 3), (32, 3, 2), (34, 4, 3),
        ])
        packet, _ = detect_packet(image, self.config, sequence=9)
        self.assertTrue(packet["calibrated"], packet["error"])
        by_id = {item["markerId"]: item for item in packet["tiles"]}
        self.assertEqual(by_id[32]["residenceKind"], "Standard")
        self.assertEqual(by_id[34]["residenceKind"], "Standard")
        self.assertNotIn("residenceKind", by_id[10])

    def test_all_49_marker_identities_map_to_their_cells(self):
        cells = [(column, row) for row in range(7) for column in range(7)]
        cells.remove((3, 3))
        placement = [(10, 3, 3)] + [
            (marker_id, column, row)
            for marker_id, (column, row) in zip(range(11, 59), cells)
        ]
        packet, _ = detect_packet(self.board_image(placement), self.config,
                                  sequence=10)
        self.assertTrue(packet["calibrated"], packet["error"])
        self.assertEqual(len(packet["tiles"]), 49)
        observed = {
            item["markerId"]: (item["column"], item["row"])
            for item in packet["tiles"]
        }
        self.assertEqual(observed, {
            marker_id: (column, row)
            for marker_id, column, row in placement
        })

    def test_new_markers_59_to_89_map_as_a_second_board_subset(self):
        cells = [(column, row) for row in range(7) for column in range(7)]
        cells.remove((3, 3))
        placement = [(10, 3, 3)] + [
            (marker_id, column, row)
            for marker_id, (column, row) in zip(range(59, 90), cells)
        ]
        packet, _ = detect_packet(self.board_image(placement), self.config,
                                  sequence=11)
        self.assertTrue(packet['calibrated'], packet['error'])
        self.assertEqual(len(packet['tiles']), 32)
        self.assertEqual({
            item['markerId']: (item['column'], item['row'])
            for item in packet['tiles']
        }, {
            marker_id: (column, row)
            for marker_id, column, row in placement
        })

    def test_missing_corner_is_not_calibrated(self):
        image = self.board_image([(10, 3, 3)], omitted_corner=3)

        packet, _ = detect_packet(image, self.config, sequence=3)

        self.assertFalse(packet["calibrated"])
        self.assertEqual(packet["cornerCount"], 3)
        self.assertEqual(packet["tiles"], [])

    def test_duplicate_physical_marker_id_is_rejected(self):
        image = self.board_image([(10, 3, 3), (12, 3, 2), (12, 2, 3)])

        packet, _ = detect_packet(image, self.config, sequence=4)

        self.assertFalse(packet["calibrated"])
        self.assertIn("Duplicate physical marker IDs", packet["error"])
        self.assertEqual(packet["tiles"], [])

    def test_three_millimetre_outward_corner_offset_preserves_cell_mapping(self):
        requested = [
            (10, 3, 3),
            (11, 0, 0),
            (12, 6, 0),
            (13, 0, 6),
            (14, 6, 6),
            (15, 3, 0),
        ]
        cell_size_px = 900.0 / 7.0
        offset_px = round((3.0 / 43.0) * cell_size_px)
        image = self.board_image(requested, calibration_offset_px=offset_px)

        packet, _ = detect_packet(image, self.config, sequence=5)

        self.assertTrue(packet["calibrated"], packet["error"])
        observed = {
            item["markerId"]: (item["column"], item["row"])
            for item in packet["tiles"]
        }
        self.assertEqual(
            observed,
            {marker_id: (column, row) for marker_id, column, row in requested},
        )

    def test_provisional_fifteen_millimetre_corner_code_pixel_scale(self):
        # At the documented 450 mm starting height, a 15 mm marker code is
        # estimated at roughly 36 pixels in a 1080-line, no-crop frame.
        image = self.board_image(
            [(10, 3, 3)],
            corner_marker_size=36,
            tile_marker_size=57,
        )

        packet, _ = detect_packet(image, self.config, sequence=6)

        self.assertTrue(packet["calibrated"], packet["error"])
        self.assertEqual(packet["cornerCount"], 4)
        self.assertEqual((packet["tiles"][0]["column"], packet["tiles"][0]["row"]),
                         (3, 3))

    def board_image(
        self,
        tiles,
        omitted_corner=None,
        calibration_offset_px=0,
        corner_marker_size=120,
        tile_marker_size=66,
    ):
        image = np.full((1200, 1200, 3), 255, dtype=np.uint8)
        board_min = 150.0
        board_max = 1050.0
        marker_size = corner_marker_size
        offset = calibration_offset_px
        corner_origins = {
            0: (round(board_min - marker_size - offset),
                round(board_min - marker_size - offset)),
            1: (round(board_max + offset),
                round(board_min - marker_size - offset)),
            2: (round(board_max + offset), round(board_max + offset)),
            3: (round(board_min - marker_size - offset),
                round(board_max + offset)),
        }
        for marker_id, (x, y) in corner_origins.items():
            if marker_id != omitted_corner:
                self.paste_marker(image, marker_id, x, y, marker_size)

        cell_size = 900.0 / 7.0
        tile_size = tile_marker_size
        for marker_id, column, row in tiles:
            center_x = board_min + (column + 0.5) * cell_size
            center_y = board_min + (row + 0.5) * cell_size
            self.paste_marker(
                image,
                marker_id,
                int(round(center_x - tile_size / 2)),
                int(round(center_y - tile_size / 2)),
                tile_size,
            )
        return image

    def paste_marker(self, image, marker_id, x, y, size):
        marker = cv2.aruco.generateImageMarker(self.dictionary, marker_id, size)
        image[y:y + size, x:x + size] = cv2.cvtColor(marker, cv2.COLOR_GRAY2BGR)


if __name__ == "__main__":
    unittest.main()
