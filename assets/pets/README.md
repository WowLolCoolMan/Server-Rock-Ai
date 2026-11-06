# Companion sprite sheets

Built-in companion sheets use a lightweight 4×4 atlas. The first seven runtime states use these cells:

| State | Cell (zero-based) |
| --- | ---: |
| idle | 0 |
| thinking | 4 |
| coding | 6 |
| coding_malicious | 7 |
| stuck | 11 |
| success | 12 |
| error | 8 |

The native app ships pre-cropped state frames so it does not decode or animate a full sheet at runtime. Users can still assign local SVG/PNG/WebP frames or sheets from the pet customization dialog; custom files remain local to the device.
