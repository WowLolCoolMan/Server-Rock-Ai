// Pet wandering: while idle, the pet walks around the window on a 10 × 10 grid, one
// neighboring cell at a time, resting 2–6 s at each destination.
// Why: requested "wander" behavior. The grid, blocked cells, paths and the glide live in
// ServerRock.Core (PetWanderPlanner.cs, tested); this file measures the window, moves the
// card, and adds the walking motion (bob, tilt, squash, facing the direction of travel).
// Rules: never stands over the composer or Send; stops smoothly when the state leaves idle and
// resumes ~5 s after it returns; pauses 30 s after the person moves the pet; nothing runs
// while the pet is hidden, the window is hidden, Reduce motion is on, or wander is off (it
// rides on the pet animation timer, which is stopped then).
// Depends on: MainPage.CompanionAnimation.cs (timer, CompanionTick), CompanionState.Wander.
// Called by: CompanionTick, PositionCompanion (resize hook) and the pet chooser dialog.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private const double CompanionWalkSpeed = 90;          // pixels per second
    private const double CompanionWanderResumeSeconds = 5;  // after returning to idle
    private const double CompanionWanderDragPauseSeconds = 30;
    private const double CompanionWalkStopSeconds = 0.3;    // deceleration when interrupted
    private const double CompanionTurnSeconds = 0.18;       // flip to face the other way

    private readonly PetWanderPlanner _companionWanderPlanner = new(new Random());
    private PetWalk? _companionWalk;
    private double _companionWalkStartedAt;
    private double _companionWanderNextAt = double.NegativeInfinity;
    private PetPoint? _companionWanderPlaced;
    private string _companionWanderLastState = "idle";
    // Interrupted walk: slide from here along the direction of travel to a stop.
    private PetPoint _companionStopFrom;
    private PetPoint _companionStopDirection;
    private double _companionStopStartedAt = double.NegativeInfinity;
    private double _companionFacing = 1;        // current horizontal scale sign, eased
    private double _companionFacingTarget = 1;
    private (double, double, double, double) _companionWanderLayoutSize;

    // Off by default while the feature is being built; see docs/PET_OVERHAUL.md.
    private static bool CompanionWanderDefault => false;

    private bool CompanionWanderEnabled => _companionState.Wander ?? CompanionWanderDefault;

    /// <summary>True when wander moves the pet, so the static positioning must not snap it back.</summary>
    private bool CompanionWanderActive => CompanionWanderEnabled && CompanionShouldAnimate;

    private bool CompanionIsWalking => _companionWalk is not null || CompanionIsStopping;

    private bool CompanionIsStopping => CompanionNow - _companionStopStartedAt < CompanionWalkStopSeconds;

    // One wander step per animation tick. Returns the walking pose to layer over the state pose,
    // or null when the pet is standing.
    private CompanionPose? CompanionWanderTick(double now)
    {
        if (!CompanionWanderEnabled) return null;
        var state = _companionState.CurrentState;
        if (state != _companionWanderLastState)
        {
            if (state != "idle") StopCompanionWalk(now);
            else _companionWanderNextAt = Math.Max(_companionWanderNextAt, now + CompanionWanderResumeSeconds);
            _companionWanderLastState = state;
        }

        // The person moved the pet (drag, arrow keys) or the layout did: drop the walk and wait.
        var position = CurrentCompanionPosition();
        if (_isCompanionDragging || (_companionWanderPlaced is { } placed && Distance(placed, position) > 0.5))
        {
            _companionWalk = null;
            _companionStopStartedAt = double.NegativeInfinity;
            _companionWanderPlaced = null;
            _companionWanderNextAt = now + CompanionWanderDragPauseSeconds;
            return null;
        }

        if (CompanionIsStopping)
        {
            var progress = (now - _companionStopStartedAt) / CompanionWalkStopSeconds;
            // Constant deceleration from walking speed to zero.
            var travelled = CompanionWalkSpeed * CompanionWalkStopSeconds * (progress - progress * progress / 2);
            PlaceCompanion(new PetPoint(_companionStopFrom.X + _companionStopDirection.X * travelled, _companionStopFrom.Y + _companionStopDirection.Y * travelled), clamp: true);
            return WalkPose(now, 1 - progress);
        }

        if (_companionWalk is { } walk)
        {
            var elapsed = now - _companionWalkStartedAt;
            PlaceCompanion(walk.PositionAt(elapsed), clamp: false);
            var facing = walk.FacingAt(elapsed);
            if (facing != 0) _companionFacingTarget = facing;
            if (walk.IsFinished(elapsed))
            {
                _companionWalk = null;
                _companionWanderNextAt = now + _companionWanderPlanner.NextRest().TotalSeconds;
                return null;
            }
            return WalkPose(now, 1);
        }

        if (state == "idle" && now >= _companionWanderNextAt) StartCompanionWalk(now);
        return null;
    }

    private void StartCompanionWalk(double now)
    {
        if (!TryGetCompanionWanderGrid(out var grid, out var blocked))
        {
            _companionWanderNextAt = now + 2;
            return;
        }
        var start = CurrentCompanionPosition();
        var from = grid.CellForPetPosition(start);
        if (!_companionWanderPlanner.TryPlanWalk(from, blocked, out _, out var path) || path.Count == 0)
        {
            _companionWanderNextAt = now + _companionWanderPlanner.NextRest().TotalSeconds;
            return;
        }
        var walk = new PetWalk(start, path.Select(grid.PetPositionFor).ToList(), CompanionWalkSpeed);
        if (walk.Length < 1)
        {
            _companionWanderNextAt = now + _companionWanderPlanner.NextRest().TotalSeconds;
            return;
        }
        _companionWalk = walk;
        _companionWalkStartedAt = now;
        _companionWanderPlaced = start;
    }

    // Leaving idle mid-walk: keep moving the way it was going and slow to a stop.
    private void StopCompanionWalk(double now)
    {
        if (_companionWalk is not { } walk) return;
        var elapsed = now - _companionWalkStartedAt;
        var here = walk.PositionAt(elapsed);
        var ahead = walk.PositionAt(elapsed + 0.05);
        var length = Distance(here, ahead);
        _companionWalk = null;
        if (length < 0.01) return;
        _companionStopFrom = here;
        _companionStopDirection = new PetPoint((ahead.X - here.X) / length, (ahead.Y - here.Y) / length);
        _companionStopStartedAt = now;
    }

    // The walking motion: a bob per step, a slight rock and squash on each footfall.
    // strength fades it out while stopping.
    private static CompanionPose WalkPose(double now, double strength)
    {
        const double StepSeconds = 0.32;
        var phase = now / StepSeconds * Math.PI;
        var lift = Math.Abs(Math.Sin(phase));
        var squash = 1 - lift;
        return new CompanionPose(
            0,
            -3.5 * lift * strength,
            3 * Math.Sin(phase) * strength,
            1 + 0.03 * squash * strength,
            1 - 0.04 * squash * strength);
    }

    // Combines the state's pose with walking and facing. While walking the idle breathing is
    // replaced by the walk; the horizontal flip turns through zero instead of snapping.
    private CompanionPose ComposeCompanionPose(CompanionPose statePose, CompanionPose? walkPose, double tickSeconds)
    {
        var step = tickSeconds / CompanionTurnSeconds * 2;
        _companionFacing = _companionFacing < _companionFacingTarget
            ? Math.Min(_companionFacingTarget, _companionFacing + step)
            : Math.Max(_companionFacingTarget, _companionFacing - step);
        var pose = walkPose ?? statePose;
        // Never fully zero width mid-turn: the sprite would vanish for a frame.
        var facing = Math.Abs(_companionFacing) < 0.08 ? Math.CopySign(0.08, _companionFacingTarget) : _companionFacing;
        return pose with { ScaleX = pose.ScaleX * facing, Rotation = pose.Rotation * Math.Sign(facing) };
    }

    private bool TryGetCompanionWanderGrid(out PetWanderGrid grid, out HashSet<PetGridCell> blocked)
    {
        var width = CompanionOverlay.ActualWidth;
        var height = CompanionOverlay.ActualHeight;
        var size = new PetPoint(CompanionCard.ActualWidth > 0 ? CompanionCard.ActualWidth : CompanionCard.Width, CompanionCard.ActualHeight);
        grid = new PetWanderGrid(width, height, size);
        blocked = [];
        if (!grid.IsUsable || size.X <= 0 || size.Y <= 0) return false;
        // Never stand over the composer (its status line, toolbar, prompt box and Send button).
        var obstacles = new List<PetRect>();
        foreach (var element in new FrameworkElement[] { ComposerToolbarControl, SendButton, StopButton })
        {
            if (element.Visibility == Visibility.Visible && TryGetVisualRect(element, out var rect))
                obstacles.Add(new PetRect(rect.X, rect.Y, rect.Width, rect.Height));
        }
        blocked = grid.BlockedCells(obstacles);
        return true;
    }

    // Resize while wandering: recompute the grid, keep the pet inside, and replan from there.
    private void ClampCompanionForWander()
    {
        // PositionCompanion also runs on every state change; only a real size change matters.
        var size = (CompanionOverlay.ActualWidth, CompanionOverlay.ActualHeight, CompanionCard.ActualWidth, CompanionCard.ActualHeight);
        if (size == _companionWanderLayoutSize) return;
        _companionWanderLayoutSize = size;
        if (!TryGetCompanionWanderGrid(out var grid, out var blocked)) return;
        var position = grid.ClampPetPosition(CurrentCompanionPosition());
        _companionWalk = null;
        _companionStopStartedAt = double.NegativeInfinity;
        PlaceCompanion(position, clamp: false);
        // If the smaller window put the pet over the composer, walk off it right away.
        var onBlockedCell = blocked.Contains(grid.CellForPetPosition(position));
        _companionWanderNextAt = Math.Min(_companionWanderNextAt, CompanionNow + (onBlockedCell ? 0.2 : 1));
    }

    private PetPoint CurrentCompanionPosition()
    {
        var left = Canvas.GetLeft(CompanionCard);
        var top = Canvas.GetTop(CompanionCard);
        return new PetPoint(double.IsFinite(left) ? left : 8, double.IsFinite(top) ? top : 8);
    }

    private void PlaceCompanion(PetPoint position, bool clamp)
    {
        if (clamp && TryGetCompanionWanderGrid(out var grid, out _)) position = grid.ClampPetPosition(position);
        Canvas.SetLeft(CompanionCard, position.X);
        Canvas.SetTop(CompanionCard, position.Y);
        _companionWanderPlaced = position;
    }

    private static double Distance(PetPoint first, PetPoint second) =>
        Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));

    // "Let the pet wander" in the pet chooser. Saved in companion.json (Wander).
    private CheckBox CreateCompanionWanderToggle()
    {
        var toggle = new CheckBox { Content = "Let the pet wander", IsChecked = CompanionWanderEnabled, Margin = new Thickness(0, 6, 0, 0) };
        AutomationProperties.SetHelpText(toggle, "When the pet is idle it walks around the window, avoiding the message composer. Off while Reduce motion is on.");
        toggle.Click += (_, _) =>
        {
            _companionState = _companionState with { Wander = toggle.IsChecked == true };
            SaveCompanionState();
            _companionWanderNextAt = CompanionNow + 1;
            if (!CompanionWanderEnabled) StopCompanionWalk(CompanionNow);
            RuntimeStatus.Text = CompanionWanderEnabled ? "The pet will wander when idle." : "The pet stays where you put it.";
        };
        return toggle;
    }
}
