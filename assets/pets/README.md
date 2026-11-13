# Companion sprite sheets

Each built-in pet has one sprite sheet (`<pet>-sprite-sheet.png`) and seven pre-cut state frames
(`<pet>-<state>.png`, 256×256, transparent). The app only loads the pre-cut frames, so it never
decodes a full sheet at runtime. All motion (breathing, bobbing, shaking, walking) is drawn by the
app with transforms on top of these frames; the sheets have no dedicated walk cells.

The sheets do not share one layout: Rocky's sheet is a 3×3 grid, the others are 4×4, and each
sheet places its poses differently. Cells are numbered from zero, left to right, top to bottom.

| Pet | Grid | idle | thinking | coding | coding_malicious | stuck | success | error |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| percy | 4×4 | 0 | 8 | 9 | 10 | 12 | 13 | 14 |
| rocky | 3×3 | 0 | 2 | 3 | 5 | 8 | 6 | 7 |
| byte | 4×4 | 0 | 4 | 6 | 8 | 10 | 14 | 12 |
| sprout | 4×4 | 0 | 4 | 8 | 10 | 11 | 14 | 12 |
| nova | 4×4 | 0 | 4 | 6 | 7 | 11 | 13 | 14 |
| ember | 4×4 | 0 | 4 | 6 | 9 | 10 | 13 | 15 |

To re-cut the frames after changing a sheet or this table, edit the `MAP` in `cut-frames.sh`
(keep it in step with the table above) and run `sh assets/pets/cut-frames.sh` (needs ImageMagick 7).
Each frame is the whole cell minus a thin edge band, padded to a square, so the character stays in
the same spot in every state and cross-fades line up.

Users can still assign local SVG/PNG/JPEG/WebP frames from the pet customization dialog; custom
files stay on the device.

Percy is the official Skai Desktop mascot and the default companion for new profiles. His
generated 4×4 atlas also includes four walk-cycle frames in row 2 (cells 4–7); the current
companion runtime uses the seven state frames and its existing transform animation for motion.
