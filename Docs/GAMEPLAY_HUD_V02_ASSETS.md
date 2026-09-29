# Gameplay HUD v02 art notes

The four transparent full-body animal portraits were generated with the built-in image-generation tool from the user-provided `SYMBIOSIS_49_animal_portrait_sample_v02.png` as a visual reference. These are concise, reusable prompts describing the final art direction, not verbatim transcripts of the generation requests:

- Pigeon: “A single complete city pigeon, standing in side view, matching the reference's softly faceted painted game-art style, gray layered feathers with subtle teal and purple neck iridescence, pink feet, isolated on a fully transparent background, no ring, badge, shadow, lettering or other objects.”
- Squirrel: “A single complete red squirrel, sitting in side view with a large arched bushy tail, matching the reference's softly faceted painted game-art style, warm copper fur and cream chest, isolated on a fully transparent background, no ring, badge, shadow, lettering or other objects.”
- Hedgehog: “A single complete hedgehog in side view, matching the reference's softly faceted painted game-art style, layered brown and cream spines, pale face and tiny dark feet, isolated on a fully transparent background, no ring, badge, shadow, lettering or other objects.”
- Fox: “A single complete red fox in side view, matching the reference's softly faceted painted game-art style, orange coat, cream throat and tail tip, dark legs, isolated on a fully transparent background, no ring, badge, shadow, lettering or other objects.”

The day/night illustration is the user's supplied reference image `exec-d8e785a1-8dcf-49ee-a07c-82f0058da7bc.png`, copied without repainting. Its clock progress pointer, the animal badge rims and progress arcs, and all numbers are native Unity UI so they stay dynamic. The badge arc shows living animals as a share of the current species total; it occupies 80% of the circular track at full population to match the sample's visual rhythm. The sample's numbers are not used as gameplay defaults.

If a whole species is temporarily dead, its badge first uses the red death state while the death visual remains, then switches to cyan and displays the real seconds until the first animal respawns. No fixed example countdown is baked into the asset.

Runtime files: `Assets/Resources/UI/GameplayHud/*-v02.png`. The prior `*-v01.png` portraits remain available for comparison.
