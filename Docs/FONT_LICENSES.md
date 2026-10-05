# UI fonts

The game packages the following fonts under `Assets/Resources/Fonts` so the
English UI does not depend on fonts installed on the player's computer.

| Font asset | Purpose | Source | License |
| --- | --- | --- | --- |
| `Nunito-Regular.ttf`, `Nunito-Bold.ttf` | Primary UI font (English and shared Latin text) | [Google Fonts Nunito](https://github.com/google/fonts/tree/main/ofl/nunito) | SIL Open Font License 1.1; see `OFL-Nunito.txt` |
| `NotoSansSC-Regular.ttf` | Simplified Chinese glyph fallback | [Google Fonts Noto Sans SC](https://github.com/google/fonts/tree/main/ofl/notosanssc) | SIL Open Font License 1.1; see `OFL-NotoSansSC.txt` |

The regular TTFs were generated from the official `wght=400` variable font
files, and Nunito Bold from `wght=700`, with fontTools varLib instancer. The
original license notices accompany the font assets. Both Nunito importers list
Noto Sans SC as a fallback font and hold a reference to the packaged Noto font.
