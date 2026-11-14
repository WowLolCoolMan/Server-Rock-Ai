const assert = require("node:assert/strict");
const { PetWanderGrid, PetWanderPlanner, PetWalk } = require("../apps/desktop/electron/pet-wander.cjs");

const grid = new PetWanderGrid(1440, 900, { x: 220, y: 190 }, 16);
assert.equal(grid.isUsable, true);
assert.deepEqual(grid.clampPetPosition({ x: -100, y: 10_000 }), { x: 16, y: 694 });

const blocked = grid.blockedCells([{ x: 500, y: 360, width: 440, height: 180 }]);
assert.ok(blocked.size > 0);

const planner = new PetWanderPlanner(() => 0.75);
const from = grid.cellForPetPosition({ x: 16, y: 16 });
const plan = planner.tryPlanWalk(from, blocked);
assert.equal(plan.planned, true);
assert.ok(plan.path.length > 0);
let previous = from;
for (const step of plan.path) {
  assert.equal(PetWanderPlanner.areNeighbors(previous, step), true);
  assert.equal(blocked.has(`${step.column}:${step.row}`), false);
  previous = step;
}

const walk = new PetWalk({ x: 16, y: 16 }, plan.path.map((cell) => grid.petPositionFor(cell)), 90);
assert.ok(walk.duration > 0);
assert.deepEqual(walk.positionAt(0), { x: 16, y: 16 });
assert.ok(Number.isFinite(walk.positionAt(walk.duration / 2).x));
assert.equal(walk.isFinished(walk.duration + 1), true);

console.log("Pet wander adapter checks passed.");
