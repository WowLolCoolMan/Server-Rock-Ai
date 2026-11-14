import { useEffect, useMemo, useState, type PointerEvent } from "react";
import type { PetActivity, PetManifest, PetWindowConfig } from "../../../shared/contracts";

const defaultManifest: PetManifest = {
  frameWidth: 256,
  frameHeight: 256,
  columns: 6,
  rows: 8,
  states: {
    idle: { row: 0, frames: [0, 1, 2, 3, 4, 5], fps: 2 },
    thinking: { row: 1, frames: [0, 1, 2, 3, 4, 5], fps: 8 },
    coding: { row: 2, frames: [0, 1, 2, 3, 4, 5], fps: 10 },
    working: { row: 2, frames: [0, 1, 2, 3, 4, 5], fps: 10 },
    error: { row: 6, frames: [0, 1, 2, 3, 4, 5], fps: 4 },
    success: { row: 5, frames: [0, 1, 2, 3, 4, 5], fps: 6 },
    walking: { row: 7, frames: [0, 1, 2, 3, 4, 5], fps: 8 },
  },
};

const builtInSheets: Record<string, string> = {
  byte: new URL("../../../../../assets/pets/byte-animation-sprite-sheet.png", import.meta.url).href,
  percy: new URL("../../../../../assets/pets/percy-animation-sprite-sheet.png", import.meta.url).href,
  rocky: new URL("../../../../../assets/pets/rocky-animation-sprite-sheet.png", import.meta.url).href,
  sprout: new URL("../../../../../assets/pets/sprout-animation-sprite-sheet.png", import.meta.url).href,
  nova: new URL("../../../../../assets/pets/nova-animation-sprite-sheet.png", import.meta.url).href,
  ember: new URL("../../../../../assets/pets/ember-animation-sprite-sheet.png", import.meta.url).href,
};

const activityLabels: Record<PetActivity, string> = {
  idle: "Idle",
  walking: "Walking",
  thinking: "Thinking",
  coding: "Coding",
  working: "Working",
  success: "Done",
  error: "Needs attention",
};

function stateForActivity(activity: PetActivity, manifest: PetManifest) {
  return manifest.states[activity] ?? manifest.states.idle ?? { row: 0, frames: [0], fps: 2 };
}

function PetSprite({ config, frame }: { config: PetWindowConfig; frame: number }) {
  const manifest = config.customManifest ?? defaultManifest;
  const state = stateForActivity(config.activity, manifest);
  if (config.customSpriteDataUrl && !config.customManifest) return <img className="pet-window-custom-sprite" src={config.customSpriteDataUrl} alt="" draggable="false" />;
  const displaySize = 96;
  const frameIndex = state.frames[frame % state.frames.length] ?? 0;
  return <span className="pet-window-sprite" aria-hidden="true" style={{ width: displaySize, height: displaySize, backgroundImage: `url(${config.customSpriteDataUrl ?? builtInSheets[config.petId] ?? builtInSheets.byte})`, backgroundSize: `${manifest.columns * displaySize}px ${manifest.rows * displaySize}px`, backgroundPosition: `-${frameIndex * displaySize}px -${state.row * displaySize}px` }} />;
}

export function PetCompanion({ config, onHide }: { config: PetWindowConfig; onHide: () => void }) {
  const manifest = config.customManifest ?? defaultManifest;
  const state = stateForActivity(config.activity, manifest);
  const [frame, setFrame] = useState(0);
  const [dragging, setDragging] = useState(false);
  const framesPerSecond = state.fps;

  useEffect(() => { setFrame(0); }, [config.activity, config.petId, config.customSpriteDataUrl, config.customManifest]);
  useEffect(() => {
    if (config.reduceMotion || state.frames.length < 2) return;
    const timer = window.setInterval(() => setFrame((current) => (current + 1) % state.frames.length), 1_000 / framesPerSecond);
    return () => window.clearInterval(timer);
  }, [config.reduceMotion, framesPerSecond, state.frames.length]);

  const hint = useMemo(() => config.wander && config.activity === "idle" ? "Walking around" : activityLabels[config.activity], [config.activity, config.wander]);
  const onPointerDown = (event: PointerEvent<HTMLButtonElement>) => {
    event.currentTarget.setPointerCapture(event.pointerId);
    setDragging(true);
    window.serverRockAgent.pet.dragStart({ screenX: event.screenX, screenY: event.screenY });
  };
  const onPointerMove = (event: PointerEvent<HTMLButtonElement>) => {
    if (!dragging) return;
    window.serverRockAgent.pet.dragMove({ screenX: event.screenX, screenY: event.screenY });
  };
  const endDrag = (event: PointerEvent<HTMLButtonElement>) => {
    if (event.currentTarget.hasPointerCapture(event.pointerId)) event.currentTarget.releasePointerCapture(event.pointerId);
    setDragging(false);
    window.serverRockAgent.pet.dragEnd();
  };

  return <div className={`pet-window-shell${dragging ? " is-dragging" : ""}`}>
    <button className="pet-window-dismiss" type="button" onClick={onHide} aria-label="Hide Skai pet">×</button>
    <button className="pet-window-button" type="button" aria-label="Skai pet. Drag to move." onPointerDown={onPointerDown} onPointerMove={onPointerMove} onPointerUp={endDrag} onPointerCancel={() => { setDragging(false); window.serverRockAgent.pet.dragEnd(); }}>
      <PetSprite config={config} frame={frame} />
    </button>
    <span className="pet-window-hint">{hint}</span>
  </div>;
}
