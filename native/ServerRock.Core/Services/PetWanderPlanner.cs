// Pet wander planning: the grid, blocked cells, paths and the walking glide for the desktop
// pet. Pure math with an injected Random so it can be tested with a seed; the UI side
// (MainPage.CompanionWander.cs) only measures the window and moves the sprite.
//
// The movement area is a 10 × 10 grid over the window's content (cell = width / 10 by
// height / 10). The pet picks a random free cell like a piece on a chessboard and walks there
// one neighboring cell at a time (diagonals allowed), never jumping. A cell is blocked when the
// pet standing there would overlap an obstacle such as the composer.

namespace ServerRock.Core.Services;

/// <summary>A cell in the wander grid, counted from the top-left, zero-based.</summary>
public readonly record struct PetGridCell(int Column, int Row);

/// <summary>A point or size in device-independent pixels.</summary>
public readonly record struct PetPoint(double X, double Y);

/// <summary>An axis-aligned rectangle in device-independent pixels.</summary>
public readonly record struct PetRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public bool Intersects(PetRect other) =>
        X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

    public PetRect Inflate(double amount) => new(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);
}

/// <summary>
/// Grid geometry for one window size. The pet (a <paramref name="petSize"/> box) "stands" on a
/// cell when its box is centered on the cell's center, clamped so the box stays inside the area.
/// </summary>
public sealed class PetWanderGrid
{
    public const int Columns = 10;
    public const int Rows = 10;

    public PetWanderGrid(double width, double height, PetPoint petSize, double margin = 8)
    {
        Width = double.IsFinite(width) ? Math.Max(0, width) : 0;
        Height = double.IsFinite(height) ? Math.Max(0, height) : 0;
        PetSize = new PetPoint(Math.Max(0, petSize.X), Math.Max(0, petSize.Y));
        Margin = Math.Max(0, margin);
    }

    public double Width { get; }
    public double Height { get; }
    public PetPoint PetSize { get; }
    public double Margin { get; }
    public double CellWidth => Width / Columns;
    public double CellHeight => Height / Rows;
    public bool IsUsable => Width > 0 && Height > 0;

    public static bool Contains(PetGridCell cell) =>
        cell.Column is >= 0 and < Columns && cell.Row is >= 0 and < Rows;

    public static PetGridCell Clamp(PetGridCell cell) =>
        new(Math.Clamp(cell.Column, 0, Columns - 1), Math.Clamp(cell.Row, 0, Rows - 1));

    /// <summary>The cell under a point; points outside the area map to the nearest edge cell.</summary>
    public PetGridCell CellAt(PetPoint point)
    {
        if (!IsUsable) return new PetGridCell(0, 0);
        return Clamp(new PetGridCell((int)Math.Floor(point.X / CellWidth), (int)Math.Floor(point.Y / CellHeight)));
    }

    public PetPoint CellCenter(PetGridCell cell)
    {
        cell = Clamp(cell);
        return new PetPoint((cell.Column + 0.5) * CellWidth, (cell.Row + 0.5) * CellHeight);
    }

    /// <summary>Top-left of the pet's box when it stands on <paramref name="cell"/>.</summary>
    public PetPoint PetPositionFor(PetGridCell cell)
    {
        var center = CellCenter(cell);
        return ClampPetPosition(new PetPoint(center.X - PetSize.X / 2, center.Y - PetSize.Y / 2));
    }

    /// <summary>Keeps the pet's box inside the area (with the margin). Used after a resize too.</summary>
    public PetPoint ClampPetPosition(PetPoint position)
    {
        var maxLeft = Math.Max(Margin, Width - PetSize.X - Margin);
        var maxTop = Math.Max(Margin, Height - PetSize.Y - Margin);
        var left = double.IsFinite(position.X) ? position.X : Margin;
        var top = double.IsFinite(position.Y) ? position.Y : Margin;
        return new PetPoint(Math.Clamp(left, Margin, maxLeft), Math.Clamp(top, Margin, maxTop));
    }

    public PetRect PetRectFor(PetGridCell cell)
    {
        var position = PetPositionFor(cell);
        return new PetRect(position.X, position.Y, PetSize.X, PetSize.Y);
    }

    /// <summary>The cell the pet is standing on, judged by the center of its box.</summary>
    public PetGridCell CellForPetPosition(PetPoint position) =>
        CellAt(new PetPoint(position.X + PetSize.X / 2, position.Y + PetSize.Y / 2));

    /// <summary>Cells where the pet's box would overlap any obstacle (inflated by <paramref name="clearance"/>).</summary>
    public HashSet<PetGridCell> BlockedCells(IEnumerable<PetRect> obstacles, double clearance = 8)
    {
        var inflated = obstacles.Where(rect => rect.Width > 0 && rect.Height > 0).Select(rect => rect.Inflate(clearance)).ToList();
        var blocked = new HashSet<PetGridCell>();
        for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
            var cell = new PetGridCell(column, row);
            var rect = PetRectFor(cell);
            if (inflated.Any(rect.Intersects)) blocked.Add(cell);
        }
        return blocked;
    }
}

/// <summary>Picks wander targets and plans step-by-step paths on the 10 × 10 grid.</summary>
public sealed class PetWanderPlanner(Random random)
{
    private static readonly (int Column, int Row)[] NeighborOffsets =
        [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)];

    public const double MinimumRestSeconds = 2;
    public const double MaximumRestSeconds = 6;

    private readonly Random _random = random ?? throw new ArgumentNullException(nameof(random));

    /// <summary>True when two cells touch, including diagonally (one chess-king move).</summary>
    public static bool AreNeighbors(PetGridCell first, PetGridCell second)
    {
        var columns = Math.Abs(first.Column - second.Column);
        var rows = Math.Abs(first.Row - second.Row);
        return Math.Max(columns, rows) == 1;
    }

    /// <summary>A random rest between 2 and 6 seconds.</summary>
    public TimeSpan NextRest() =>
        TimeSpan.FromSeconds(MinimumRestSeconds + _random.NextDouble() * (MaximumRestSeconds - MinimumRestSeconds));

    /// <summary>
    /// Picks a random free cell other than <paramref name="from"/> that can be reached without
    /// crossing a blocked cell, and returns the path to it. Returns false when nothing is reachable.
    /// </summary>
    public bool TryPlanWalk(PetGridCell from, IReadOnlySet<PetGridCell> blocked, out PetGridCell target, out IReadOnlyList<PetGridCell> path)
    {
        from = PetWanderGrid.Clamp(from);
        if (blocked.Contains(from))
        {
            // Dropped (or resized) onto a blocked cell: first walk off it, straight to the
            // nearest free cell, still one neighboring cell per step.
            target = NearestFreeCell(from, blocked);
            path = StraightPath(from, target);
            return target != from;
        }
        var reachable = ReachableFrom(from, blocked);
        reachable.Remove(from);
        if (reachable.Count == 0)
        {
            target = from;
            path = [];
            return false;
        }
        // Sort first so the same seed always gives the same walk, whatever the set's order.
        var candidates = reachable.Keys.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).ToList();
        target = candidates[_random.Next(candidates.Count)];
        path = BuildPath(from, target, reachable);
        return true;
    }

    /// <summary>
    /// Shortest path from <paramref name="from"/> to <paramref name="to"/> moving one neighboring
    /// cell at a time and never entering a blocked cell. The start cell is not included; an empty
    /// list means already there or unreachable.
    /// </summary>
    public static IReadOnlyList<PetGridCell> PlanPath(PetGridCell from, PetGridCell to, IReadOnlySet<PetGridCell> blocked)
    {
        from = PetWanderGrid.Clamp(from);
        if (!PetWanderGrid.Contains(to) || blocked.Contains(to) || from == to) return [];
        var reachable = ReachableFrom(from, blocked);
        return reachable.ContainsKey(to) ? BuildPath(from, to, reachable) : [];
    }

    /// <summary>
    /// The nearest free cell to <paramref name="cell"/> (itself when free). Used when the pet is
    /// dropped or resized onto a blocked cell, so it can walk off it.
    /// </summary>
    public static PetGridCell NearestFreeCell(PetGridCell cell, IReadOnlySet<PetGridCell> blocked)
    {
        cell = PetWanderGrid.Clamp(cell);
        if (!blocked.Contains(cell)) return cell;
        var best = cell;
        var bestDistance = int.MaxValue;
        for (var row = 0; row < PetWanderGrid.Rows; row++)
        for (var column = 0; column < PetWanderGrid.Columns; column++)
        {
            var candidate = new PetGridCell(column, row);
            if (blocked.Contains(candidate)) continue;
            var distance = Math.Max(Math.Abs(column - cell.Column), Math.Abs(row - cell.Row));
            if (distance < bestDistance) (best, bestDistance) = (candidate, distance);
        }
        return best;
    }

    // King moves from one cell to another: diagonal while both coordinates differ, then straight.
    private static List<PetGridCell> StraightPath(PetGridCell from, PetGridCell to)
    {
        var path = new List<PetGridCell>();
        var current = from;
        while (current != to)
        {
            current = new PetGridCell(current.Column + Math.Sign(to.Column - current.Column), current.Row + Math.Sign(to.Row - current.Row));
            path.Add(current);
        }
        return path;
    }

    // Breadth-first search over the 8-connected grid, never entering a blocked cell.
    private static Dictionary<PetGridCell, PetGridCell> ReachableFrom(PetGridCell from, IReadOnlySet<PetGridCell> blocked)
    {
        var cameFrom = new Dictionary<PetGridCell, PetGridCell> { [from] = from };
        var queue = new Queue<PetGridCell>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var (columnOffset, rowOffset) in NeighborOffsets)
            {
                var next = new PetGridCell(current.Column + columnOffset, current.Row + rowOffset);
                if (!PetWanderGrid.Contains(next) || blocked.Contains(next) || cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }
        return cameFrom;
    }

    private static List<PetGridCell> BuildPath(PetGridCell from, PetGridCell to, Dictionary<PetGridCell, PetGridCell> cameFrom)
    {
        var path = new List<PetGridCell>();
        for (var cell = to; cell != from; cell = cameFrom[cell]) path.Add(cell);
        path.Reverse();
        return path;
    }
}

/// <summary>
/// The walking glide along a planned path: a steady speed in pixels per second, easing in on
/// the first step and out on the last, so the pet starts and stops softly and never jumps.
/// </summary>
public sealed class PetWalk
{
    private readonly PetPoint[] _points;
    private readonly double[] _cumulative;

    /// <param name="start">Where the pet is now (it may be between cells, e.g. after a drag).</param>
    /// <param name="waypoints">Pet positions for each step of the path, in order.</param>
    /// <param name="pixelsPerSecond">Cruising speed.</param>
    public PetWalk(PetPoint start, IReadOnlyList<PetPoint> waypoints, double pixelsPerSecond)
    {
        if (!(pixelsPerSecond > 0) || !double.IsFinite(pixelsPerSecond)) throw new ArgumentOutOfRangeException(nameof(pixelsPerSecond));
        _points = [start, .. waypoints];
        _cumulative = new double[_points.Length];
        for (var index = 1; index < _points.Length; index++)
            _cumulative[index] = _cumulative[index - 1] + Distance(_points[index - 1], _points[index]);
        Length = _cumulative[^1];
        Speed = pixelsPerSecond;
        // Easing in and out over the whole walk at most doubles the time at cruising speed in
        // those stretches; RampDistance bounds them so a long walk cruises steadily.
        RampDistance = Math.Min(Length / 2, Math.Max(24, Speed * 0.35));
        Duration = Length <= 0 ? 0 : (Length - 2 * RampDistance) / Speed + 2 * (2 * RampDistance / Speed);
    }

    public double Length { get; }
    public double Speed { get; }
    public double RampDistance { get; }
    public double Duration { get; }

    /// <summary>The pet position <paramref name="seconds"/> after the walk started.</summary>
    public PetPoint PositionAt(double seconds) => PointAtDistance(DistanceAt(seconds));

    /// <summary>The horizontal direction of travel at a time: -1 left, 1 right, 0 straight up/down.</summary>
    public int FacingAt(double seconds)
    {
        var before = PositionAt(Math.Max(0, seconds - 0.05));
        var after = PositionAt(Math.Min(Duration, seconds + 0.05));
        var delta = after.X - before.X;
        return Math.Abs(delta) < 0.01 ? 0 : Math.Sign(delta);
    }

    public bool IsFinished(double seconds) => seconds >= Duration;

    // Distance covered after t seconds: accelerate uniformly over RampDistance (taking twice the
    // cruise time), cruise, then decelerate uniformly over the last RampDistance.
    private double DistanceAt(double seconds)
    {
        if (Length <= 0 || seconds <= 0) return 0;
        if (seconds >= Duration) return Length;
        var rampTime = 2 * RampDistance / Speed;
        if (seconds < rampTime)
        {
            var progress = seconds / rampTime;
            return RampDistance * progress * progress;
        }
        var cruiseTime = Duration - 2 * rampTime;
        if (seconds < rampTime + cruiseTime) return RampDistance + (seconds - rampTime) * Speed;
        var remaining = (Duration - seconds) / rampTime;
        return Length - RampDistance * remaining * remaining;
    }

    private PetPoint PointAtDistance(double distance)
    {
        if (distance <= 0) return _points[0];
        if (distance >= Length) return _points[^1];
        for (var index = 1; index < _points.Length; index++)
        {
            if (distance > _cumulative[index]) continue;
            var segment = _cumulative[index] - _cumulative[index - 1];
            var amount = segment <= 0 ? 1 : (distance - _cumulative[index - 1]) / segment;
            var from = _points[index - 1];
            var to = _points[index];
            return new PetPoint(from.X + (to.X - from.X) * amount, from.Y + (to.Y - from.Y) * amount);
        }
        return _points[^1];
    }

    private static double Distance(PetPoint first, PetPoint second)
    {
        var x = second.X - first.X;
        var y = second.Y - first.Y;
        return Math.Sqrt(x * x + y * y);
    }
}
