"""Original, deterministic soundtrack and UI sounds for SYMBIOSIS:49.

Usage: python Tools/Audio/generate_symbiosis_audio.py
Only the Python standard library is used: no samples, soundfonts, or downloads.
"""

from __future__ import annotations

import argparse
from array import array
import math
from pathlib import Path
import random
import sys
import wave


RATE = 44100
BEAT = 60 / 80
BARS = 16
LOOP_SECONDS = BARS * 4 * BEAT  # 48 seconds
TAU = 2 * math.pi
DEFAULT_OUTPUT = (Path(__file__).resolve().parents[2] /
                  "Assets/Resources/Audio/Symbiosis49")
NOTES = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5,
         "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def hz(note: str) -> float:
    midi = (int(note[-1]) + 1) * 12 + NOTES[note[:-1]]
    return 440 * 2 ** ((midi - 69) / 12)


def add_wood(left: array, right: array, start: float, note: str,
             duration: float, level: float, pan: float, *, soft=False) -> None:
    """Soft felt-key or struck-wood voice, with a tail wrapped at loop seam."""
    size = len(left)
    base = round(start * RATE) % size
    frequency = hz(note)
    gain_l = level * math.sqrt((1 - pan) / 2)
    gain_r = level * math.sqrt((1 + pan) / 2)
    for i in range(round(duration * RATE)):
        t = i / RATE
        attack = min(1, t / (0.015 if soft else 0.006))
        release = max(0, min(1, (duration - t) / 0.1))
        envelope = attack * release * math.exp((-1.6 if soft else -2.7) * t)
        phase = TAU * frequency * t
        tone = (0.80 * math.sin(phase) +
                0.18 * math.sin(2.01 * phase) * math.exp(-1.4 * t) +
                0.06 * math.sin(3.97 * phase) * math.exp(-3.2 * t))
        position = (base + i) % size
        value = envelope * tone
        left[position] += value * gain_l
        right[position] += value * gain_r


def add_leaf(left: array, right: array, start: float, rng: random.Random,
             level: float) -> None:
    """Low-passed, shaped noise suggests paper/foliage, never a sample."""
    size = len(left)
    base = round(start * RATE) % size
    filtered = 0.0
    length = round(0.34 * RATE)
    for i in range(length):
        filtered += 0.075 * (rng.uniform(-1, 1) - filtered)
        envelope = math.sin(math.pi * i / length) ** 2
        position = (base + i) % size
        left[position] += filtered * envelope * level * 0.78
        right[position] += filtered * envelope * level * 0.61


def make_menu_loop() -> tuple[array, array]:
    count = round(LOOP_SECONDS * RATE)
    left = array("f", [0]) * count
    right = array("f", [0]) * count
    rng = random.Random(490049)

    # Continuous but quiet tonal bed; whole-number periods meet without a seam.
    cycles_d = round(hz("D3") * LOOP_SECONDS)
    cycles_a = round(hz("A3") * LOOP_SECONDS)
    for i in range(count):
        fraction = i / count
        sway = 0.82 + 0.18 * math.sin(TAU * fraction * 2 + 0.4)
        left[i] += 0.012 * sway * math.sin(TAU * cycles_d * fraction)
        right[i] += 0.010 * sway * math.sin(TAU * cycles_a * fraction)

    # D(add9) - Bm7 - Gmaj7 - A(sus2), four gently varied passes.
    chords = [
        (("D4", "F#4", "A4"), "D3", "A3"),
        (("B3", "D4", "F#4"), "B2", "F#3"),
        (("G3", "B3", "F#4"), "G2", "D3"),
        (("A3", "C#4", "E4"), "A2", "E3"),
    ]
    melodies = [
        ((0.50, "A4"), (2.25, "F#4"), (3.25, "E4")),
        ((0.75, "F#4"), (2.00, "E4"), (3.25, "D4")),
        ((0.25, "D4"), (1.75, "B3"), (3.00, "D4")),
        ((0.75, "C#4"), (2.00, "E4"), (3.00, "F#4")),
    ]
    for bar in range(BARS):
        start = bar * 4 * BEAT
        chord, bass, fifth = chords[bar % 4]
        variation = bar // 4
        for key, note in enumerate(chord):
            add_wood(left, right, start + key * 0.016, note, 2.25,
                     0.030 - key * 0.004, (-0.30, 0.08, 0.32)[key], soft=True)
        add_wood(left, right, start, bass, 1.55, 0.029, -0.10)
        if bar % 2:
            add_wood(left, right, start + 2 * BEAT, fifth, 1.05, 0.015, 0.12)
        for event, (beat, note) in enumerate(melodies[bar % 4]):
            if (variation == 1 and event == 1 and bar % 4 == 2) or (
                    variation == 2 and event == 2 and bar % 4 == 0):
                continue
            timing = ((bar * 5 + event * 3) % 5 - 2) * 0.007
            velocity = 0.040 * (0.86 + ((bar + event * 2) % 4) * 0.055)
            add_wood(left, right, start + beat * BEAT + timing, note, 1.27,
                     velocity, -0.20 if event % 2 else 0.23)
        add_leaf(left, right, start + 1.5 * BEAT, rng,
                 0.010 if bar % 2 else 0.007)

    # Two circular reflections suggest a small tabletop space and preserve
    # tails across the boundary. They are deliberately subtle.
    dry_l, dry_r = array("f", left), array("f", right)
    delay_a, delay_b = round(0.109 * RATE), round(0.271 * RATE)
    for i in range(count):
        left[i] += dry_r[(i - delay_a) % count] * 0.13
        left[i] += dry_l[(i - delay_b) % count] * 0.055
        right[i] += dry_l[(i - delay_a) % count] * 0.13
        right[i] += dry_r[(i - delay_b) % count] * 0.055
    return left, right


def tap(data: array, start: float, pitch: float, strength: float,
        rng: random.Random, duration=0.16) -> None:
    base = round(start * RATE)
    for i in range(round(duration * RATE)):
        position = base + i
        if position >= len(data):
            break
        t = i / RATE
        envelope = (1 - math.exp(-170 * t)) * math.exp(-34 * t)
        wood = math.sin(TAU * pitch * t) * 0.78
        wood += math.sin(TAU * pitch * 1.9 * t) * 0.20
        data[position] += strength * envelope * (wood + rng.uniform(-0.085, 0.085))


def swish(data: array, start: float, duration: float, strength: float,
          rng: random.Random, reverse=False) -> None:
    base = round(start * RATE)
    filtered = 0.0
    for i in range(round(duration * RATE)):
        position = base + i
        if position >= len(data):
            break
        progress = i / (duration * RATE)
        filtered += 0.075 * (rng.uniform(-1, 1) - filtered)
        envelope = math.sin(math.pi * progress) ** 1.8
        tilt = 0.65 + 0.35 * (1 - progress if reverse else progress)
        data[position] += filtered * envelope * tilt * strength


def tine(data: array, start: float, note: str, duration: float,
         strength: float) -> None:
    base = round(start * RATE)
    frequency = hz(note)
    for i in range(round(duration * RATE)):
        position = base + i
        if position >= len(data):
            break
        t = i / RATE
        envelope = min(1, t / 0.008) * math.exp(-6 * t)
        envelope *= max(0, min(1, (duration - t) / 0.04))
        data[position] += strength * envelope * (
            math.sin(TAU * frequency * t) +
            0.18 * math.sin(TAU * frequency * 2.01 * t))


def make_effect(name: str, duration: float, seed: int) -> array:
    data = array("f", [0]) * round(duration * RATE)
    rng = random.Random(seed)
    if name == "CaseOpen":
        tap(data, 0.035, 175, 0.38, rng)
        swish(data, 0.07, 0.78, 0.50, rng)
        tine(data, 0.16, "D3", 0.63, 0.11)
        tap(data, 0.73, 242, 0.24, rng)
        tap(data, 0.91, 315, 0.17, rng)
    elif name == "CaseClose":
        swish(data, 0.025, 0.54, 0.46, rng, reverse=True)
        tine(data, 0.05, "A2", 0.44, 0.07)
        tap(data, 0.48, 153, 0.40, rng)
        tap(data, 0.60, 206, 0.19, rng)
    elif name == "UiSelect":
        tap(data, 0.008, 350, 0.37, rng)
    elif name == "UiCancel":
        swish(data, 0.005, 0.13, 0.30, rng, reverse=True)
        tap(data, 0.07, 240, 0.24, rng)
    elif name == "BuildPreview":
        swish(data, 0.006, 0.17, 0.26, rng)
        tap(data, 0.15, 290, 0.20, rng)
    elif name == "BuildConfirm":
        tap(data, 0.025, 215, 0.40, rng)
        tap(data, 0.16, 333, 0.28, rng)
        tine(data, 0.16, "D5", 0.21, 0.05)
    elif name == "NextDay":
        swish(data, 0.012, 0.29, 0.32, rng)
        tap(data, 0.24, 260, 0.25, rng)
        tine(data, 0.28, "A4", 0.34, 0.07)
        tine(data, 0.39, "D5", 0.26, 0.06)
    elif name == "RestartConfirm":
        tap(data, 0.018, 300, 0.36, rng)
        tap(data, 0.18, 198, 0.30, rng)
        tine(data, 0.19, "D4", 0.26, 0.045)
    else:
        raise ValueError(name)
    for i in range(round(0.002 * RATE)):
        data[i] *= i / (0.002 * RATE)
    for i in range(round(0.025 * RATE)):
        data[-1 - i] *= i / (0.025 * RATE)
    return data


def write_wav(path: Path, channels: tuple[array, ...],
              target_peak: float) -> tuple[float, float]:
    size = len(channels[0])
    if any(len(channel) != size for channel in channels):
        raise ValueError("Channel length mismatch")
    peak = max(max(abs(sample) for sample in channel) for channel in channels)
    scale = target_peak / peak if peak else 1
    pcm = array("h")
    energy = 0.0
    for i in range(size):
        for channel in channels:
            value = max(-1, min(1, channel[i] * scale))
            energy += value * value
            pcm.append(round(value * 32767))
    if sys.byteorder != "little":
        pcm.byteswap()
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as output:
        output.setnchannels(len(channels))
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(pcm.tobytes())
    return target_peak, math.sqrt(energy / (size * len(channels)))


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    output = parser.parse_args().output
    music = make_menu_loop()
    peak, rms = write_wav(output / "MenuLoop.wav", music, 0.36)
    print(f"MenuLoop.wav: stereo, {LOOP_SECONDS:g}s, peak {peak:.3f}, RMS {rms:.3f}")
    effects = (
        ("CaseOpen", 1.10), ("CaseClose", 0.76),
        ("UiSelect", 0.14), ("UiCancel", 0.22),
        ("BuildPreview", 0.27), ("BuildConfirm", 0.43),
        ("NextDay", 0.72), ("RestartConfirm", 0.51),
    )
    for index, (name, duration) in enumerate(effects):
        samples = make_effect(name, duration, 4900 + index)
        peak, rms = write_wav(output / f"{name}.wav", (samples,), 0.50)
        print(f"{name}.wav: mono, {duration:.2f}s, peak {peak:.3f}, RMS {rms:.3f}")


if __name__ == "__main__":
    main()
