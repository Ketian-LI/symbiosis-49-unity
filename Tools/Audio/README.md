# SYMBIOSIS:49 original audio

Run python Tools/Audio/generate_symbiosis_audio.py from any directory to
regenerate the nine WAV files under Assets/Resources/Audio/Symbiosis49.
The composition and effects are synthesized from notes, sine waves and seeded
noise in this script. No recordings, samples, soundfonts or third-party music
are included. The generator uses only the Python standard library.

MenuLoop.wav is a 48-second, 80 BPM, 16-bar stereo loop in a calm
D-major/B-minor palette. Warm felt/wooden key notes and a very soft leaf-paper
texture suit the printed city-and-forest board-game case. Its sample tails are
wrapped through the loop boundary. The other files are short mono cues:
CaseOpen, CaseClose, UiSelect, UiCancel, BuildPreview,
BuildConfirm, NextDay, and RestartConfirm.

Suggested 2D mix: music source volume 0.28–0.35; interaction cue volume
0.35–0.55; case cues 0.4–0.6; global master 0.8. Play a cue on the committed
event, not continuously during pointer hover, and avoid retriggering it during
scene transitions. Keep the music source alive while moving between menu and
board; fade it to about 60% during the case animation, then return to the
gameplay level. All sources must respect the existing master-volume/mute
preference. Avoid distress sounds for animals.

Source format is PCM16 WAV at 44.1 kHz. For a distributed build, Unity may
compress the 48-second loop (for example Vorbis, streaming) while keeping
short cues in memory. This is an original generated work for this project, not
a claim that third-party sound libraries are royalty-free or that legal review
is unnecessary.
