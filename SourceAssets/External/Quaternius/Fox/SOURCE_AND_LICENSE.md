# Quaternius fox candidate

- Author: Quaternius
- Source model: https://poly.pizza/m/Bc97C66HKi
- Original pack: https://quaternius.com/packs/ultimateanimatedanimals.html
- License stated by both source pages: CC0 / public domain. Attribution is not required, but retained here for provenance.
- Original download: `QuaterniusFox_v01.glb` (unaltered, animation rig included)
- Editable Blender import: `QuaterniusFox_v01.blend`
- Unity FBX export: `Assets/Resources/Animals/Fox/QuaterniusFox_v01.fbx`
- Conversion script: `Tools/Blender/import_quaternius_fox.py`

The new FBX is separate from `Fox_Animated_v01.fbx`. Game states map to the model's own rigged clips: Rest → Idle, Trot → Walk, Sniff → Eating, Alert → Idle_2. This is an animation-state rebinding, not retargeting the old Blender fox's skeleton. If the new asset or a required clip fails to load, `FoxDemoVisual` falls back to the original fox.

Other candidates checked on 2026-09-30:

- Pigeon (https://poly.pizza/m/9NGlBTpDEr): CC0 and animated, but the purple, chick-like appearance conflicts with the game's urban pigeon; not installed.
- Alternative pigeon (https://sketchfab.com/3d-models/animated-bird-pigeon-797d27b68af3453e865149435df6aa30): free under CC BY with an openly linked Blender source. Its source has idle, flight, takeoff and landing but no walk clip; the Rigify control armature has 348 bones, so it is not a drop-in replacement for ground roaming.
- Hedgehog (https://poly.pizza/m/Uu0PXwt2nU): CC0 and animated, but only Idle/Attack/Death were present (no walk) and its cubic appearance was a poor fit; not installed.
- Squirrel: no freely downloadable, style-compatible model with a confirmed complete rig and locomotion clip was found in this pass. The existing editable project squirrel remains in use.
