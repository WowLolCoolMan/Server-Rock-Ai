#!/bin/sh
# Re-cuts the per-state frames (<pet>-<state>.png) from each pet's sprite sheet.
# Needs ImageMagick 7 (`magick`). Run from anywhere: sh assets/pets/cut-frames.sh
#
# Why: the shipped frames had been cut with a 4x4 grid for every pet, but Rocky's sheet is
# 3x3, and each sheet puts its poses in different cells. Every frame is now the whole cell
# (only a thin edge band is trimmed, so the character stays at the same spot in every state and cross-fades line
# up), with near-invisible background specks removed, padded to a square and scaled to 256 px.
set -eu
cd "$(dirname "$0")"

# pet  grid  idle thinking coding coding_malicious stuck success error   (cells are zero-based)
MAP="
rocky  3 0 2 3 5 8 6 7
byte   4 0 4 6 8 10 14 12
sprout 4 0 4 8 10 11 14 12
nova   4 0 4 6 7 11 13 14
ember  4 0 4 6 9 10 13 15
"
STATES="idle thinking coding coding_malicious stuck success error"

echo "$MAP" | while read -r pet grid c0 c1 c2 c3 c4 c5 c6; do
  [ -n "$pet" ] || continue
  sheet="$pet-sprite-sheet.png"
  width=$(magick identify -format '%w' "$sheet")
  height=$(magick identify -format '%h' "$sheet")
  i=0
  for cell in $c0 $c1 $c2 $c3 $c4 $c5 $c6; do
    state=$(echo $STATES | cut -d' ' -f$((i + 1)))
    col=$((cell % grid)); row=$((cell / grid))
    x=$((col * width / grid)); y=$((row * height / grid))
    w=$(((col + 1) * width / grid - x)); h=$(((row + 1) * height / grid - y))
    side=$((w > h ? w : h))
    # Skip a 3% band on every edge: neighboring poses poke a few pixels over the cell lines.
    bx=$((w * 3 / 100)); by=$((h * 3 / 100))
    magick "$sheet" -crop "$((w - 2 * bx))x$((h - 2 * by))+$((x + bx))+$((y + by))" +repage \
      -channel A -level 6%,100% +channel \
      -background none -gravity center -extent "${side}x${side}" \
      -resize 256x256 -strip "$pet-$state.png"
    i=$((i + 1))
  done
  echo "cut $pet ($grid x $grid)"
done
