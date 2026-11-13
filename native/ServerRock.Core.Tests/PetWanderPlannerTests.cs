// Pet wander planning (PetWanderGrid, PetWanderPlanner, PetWalk): grid sizes, free targets,
// blocked cells, one-neighbor steps (no teleporting), smooth glides and resize clamping.
// Uses TestSupport.Check. Run by Program.cs.

using ServerRock.Core.Services;
using static TestSupport;

internal static class PetWanderPlannerTests
{
    private static readonly PetPoint PetSize = new(110, 150);

    public static void Run()
    {
        // Grid sizes: always 10 x 10, cell = width / 10 by height / 10.
        foreach (var (width, height) in new[] { (1440.0, 900.0), (760.0, 560.0), (1164.0, 842.0), (320.0, 240.0) })
        {
            var grid = new PetWanderGrid(width, height, PetSize);
            Check(Math.Abs(grid.CellWidth - width / 10) < 1e-9 && Math.Abs(grid.CellHeight - height / 10) < 1e-9,
                $"pet wander grid is 10 x 10 cells of width/10 by height/10 at {width}x{height}");
            Check(grid.CellAt(new PetPoint(width - 0.01, height - 0.01)) == new PetGridCell(9, 9)
                && grid.CellAt(new PetPoint(-50, -50)) == new PetGridCell(0, 0)
                && grid.CellAt(new PetPoint(width * 5, height * 5)) == new PetGridCell(9, 9),
                $"pet wander grid maps points (and points outside) to cells inside the grid at {width}x{height}");
            var everyCellInside = true;
            for (var row = 0; row < PetWanderGrid.Rows; row++)
            for (var column = 0; column < PetWanderGrid.Columns; column++)
            {
                var position = grid.PetPositionFor(new PetGridCell(column, row));
                everyCellInside &= position.X >= grid.Margin - 1e-9 && position.Y >= grid.Margin - 1e-9
                    && position.X + PetSize.X <= Math.Max(width - grid.Margin, PetSize.X + grid.Margin) + 1e-9
                    && position.Y + PetSize.Y <= Math.Max(height - grid.Margin, PetSize.Y + grid.Margin) + 1e-9;
            }
            Check(everyCellInside, $"the pet stays inside the window on every cell at {width}x{height}");
        }

        // Blocked cells: a composer along the bottom blocks the cells where the pet would overlap it.
        var large = new PetWanderGrid(1440, 900, PetSize);
        var composer = new PetRect(450, 660, 820, 200);
        var blocked = large.BlockedCells([composer]);
        Check(blocked.Count > 0 && blocked.All(cell => large.PetRectFor(cell).Intersects(composer.Inflate(8)))
            && Enumerable.Range(0, 10).SelectMany(row => Enumerable.Range(0, 10).Select(column => new PetGridCell(column, row)))
                .Where(cell => !blocked.Contains(cell)).All(cell => !large.PetRectFor(cell).Intersects(composer.Inflate(8))),
            "blocked cells are exactly the cells where the pet would overlap the composer");

        // Targets and paths with a seeded random generator.
        var planner = new PetWanderPlanner(new Random(1234));
        var current = new PetGridCell(0, 0);
        var walks = 0;
        var allTargetsFree = true;
        var allStepsNeighbors = true;
        var noBlockedSteps = true;
        var restsInRange = true;
        for (var walk = 0; walk < 500; walk++)
        {
            if (!planner.TryPlanWalk(current, blocked, out var target, out var path)) break;
            walks++;
            allTargetsFree &= PetWanderGrid.Contains(target) && !blocked.Contains(target) && target != current && path.Count > 0 && path[^1] == target;
            var previous = current;
            foreach (var step in path)
            {
                allStepsNeighbors &= PetWanderPlanner.AreNeighbors(previous, step);
                noBlockedSteps &= !blocked.Contains(step);
                previous = step;
            }
            var rest = planner.NextRest().TotalSeconds;
            restsInRange &= rest is >= PetWanderPlanner.MinimumRestSeconds and <= PetWanderPlanner.MaximumRestSeconds;
            current = target;
        }
        Check(walks == 500, "the pet wander planner keeps finding reachable targets");
        Check(allTargetsFree, "pet wander targets are inside the grid, free, and the end of their path");
        Check(noBlockedSteps, "pet wander paths never step on a blocked cell");
        Check(allStepsNeighbors, "every pet wander step moves exactly one neighboring cell (no teleporting)");
        Check(restsInRange, "pet rests last between 2 and 6 seconds");

        var first = new PetWanderPlanner(new Random(42));
        var second = new PetWanderPlanner(new Random(42));
        first.TryPlanWalk(new PetGridCell(4, 4), blocked, out var firstTarget, out var firstPath);
        second.TryPlanWalk(new PetGridCell(4, 4), blocked, out var secondTarget, out var secondPath);
        Check(firstTarget == secondTarget && firstPath.SequenceEqual(secondPath), "the same seed plans the same pet walk");

        // Diagonal moves are used: a corner-to-corner path on an empty grid is 9 steps.
        var diagonal = PetWanderPlanner.PlanPath(new PetGridCell(0, 0), new PetGridCell(9, 9), new HashSet<PetGridCell>());
        Check(diagonal.Count == 9 && diagonal.Zip(diagonal.Skip(1)).All(pair => PetWanderPlanner.AreNeighbors(pair.First, pair.Second)),
            "pet wander paths move diagonally one cell per step");

        // A wall with one gap: the path goes through the gap, one cell at a time.
        var wall = Enumerable.Range(0, 10).Where(row => row != 7).Select(row => new PetGridCell(5, row)).ToHashSet();
        var around = PetWanderPlanner.PlanPath(new PetGridCell(1, 1), new PetGridCell(8, 1), wall);
        Check(around.Count > 0 && around.Contains(new PetGridCell(5, 7)) && around.All(cell => !wall.Contains(cell)),
            "pet wander paths route around blocked cells");
        Check(PetWanderPlanner.PlanPath(new PetGridCell(1, 1), new PetGridCell(5, 3), wall).Count == 0,
            "a blocked target gives no pet path");

        // Fully enclosed: nothing reachable, no walk.
        var enclosed = new HashSet<PetGridCell> { new(1, 0), new(0, 1), new(1, 1) };
        Check(!new PetWanderPlanner(new Random(7)).TryPlanWalk(new PetGridCell(0, 0), enclosed, out _, out _),
            "the pet stays put when no free cell is reachable");

        // Dropped on a blocked cell: the nearest free cell is found and the pet can walk off.
        var nearest = PetWanderPlanner.NearestFreeCell(new PetGridCell(5, 9), blocked);
        var walkedOff = new PetWanderPlanner(new Random(3)).TryPlanWalk(new PetGridCell(5, 9), blocked, out var offTarget, out var offPath);
        Check(!blocked.Contains(nearest) && walkedOff && offTarget == nearest && !blocked.Contains(offTarget)
            && offPath.Prepend(new PetGridCell(5, 9)).Zip(offPath).All(pair => PetWanderPlanner.AreNeighbors(pair.First, pair.Second)),
            "a pet dropped onto the composer finds a free cell and can walk off it");

        // Resize: the grid is recomputed and the pet is clamped inside the smaller window.
        var small = new PetWanderGrid(760, 560, PetSize);
        var farCorner = large.PetPositionFor(new PetGridCell(9, 9));
        var clamped = small.ClampPetPosition(farCorner);
        Check(clamped.X + PetSize.X <= 760 - small.Margin + 1e-9 && clamped.Y + PetSize.Y <= 560 - small.Margin + 1e-9
            && clamped.X >= small.Margin && clamped.Y >= small.Margin,
            "resizing from 1440x900 to 760x560 clamps the pet inside the window");
        Check(small.CellForPetPosition(clamped) is { Column: >= 0 and < 10, Row: >= 0 and < 10 },
            "after a resize the pet maps to a cell inside the new grid");
        var tiny = new PetWanderGrid(80, 60, PetSize).ClampPetPosition(new PetPoint(500, 500));
        Check(double.IsFinite(tiny.X) && double.IsFinite(tiny.Y), "a window smaller than the pet still gives a finite pet position");

        // The glide: steady speed, soft start and stop, and never a jump between frames.
        var start = large.PetPositionFor(new PetGridCell(0, 0));
        var waypoints = diagonal.Select(large.PetPositionFor).ToList();
        var glide = new PetWalk(start, waypoints, pixelsPerSecond: 90);
        var frame = 1.0 / 60;
        var maxStep = 0.0;
        var previousPosition = glide.PositionAt(0);
        for (var time = frame; time <= glide.Duration + frame; time += frame)
        {
            var position = glide.PositionAt(time);
            maxStep = Math.Max(maxStep, Math.Sqrt(Math.Pow(position.X - previousPosition.X, 2) + Math.Pow(position.Y - previousPosition.Y, 2)));
            previousPosition = position;
        }
        Check(maxStep <= 90 * frame + 1e-6, "the pet glide never moves faster than its walking speed (no jumps)");
        Check(glide.PositionAt(0) == start && glide.PositionAt(glide.Duration) == waypoints[^1] && glide.IsFinished(glide.Duration),
            "the pet glide starts where the pet is and ends on the target cell");
        var cruise = glide.PositionAt(glide.Duration / 2);
        var cruiseNext = glide.PositionAt(glide.Duration / 2 + frame);
        var cruiseSpeed = Math.Sqrt(Math.Pow(cruiseNext.X - cruise.X, 2) + Math.Pow(cruiseNext.Y - cruise.Y, 2)) / frame;
        var startSpeed = Math.Sqrt(Math.Pow(glide.PositionAt(frame).X - start.X, 2) + Math.Pow(glide.PositionAt(frame).Y - start.Y, 2)) / frame;
        Check(Math.Abs(cruiseSpeed - 90) < 0.5 && startSpeed < 10, "the pet glide eases in and cruises at a steady pixels-per-second speed");
        Check(glide.FacingAt(glide.Duration / 2) == 1, "the pet faces its direction of travel");
        Check(new PetWalk(start, [], 90).Duration == 0, "an empty pet walk finishes at once");
    }
}
