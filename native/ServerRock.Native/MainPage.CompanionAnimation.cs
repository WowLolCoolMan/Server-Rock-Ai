// Pet animation: gives every companion state a looping motion, cross-fades between frames,
// returns short-lived states (success, error, stuck) to idle, and times the speech bubble.
// Why: the pet used to swap a static picture with a hard cut and then stay in the last state
// forever. All motion is a transform on CompanionSprite, so it works for the built-in frames
// and for custom single-image sprites alike.
// Performance: one timer, running only while the pet is visible, not covered by the compact
// drawer, the window is visible, and Reduce motion is off. Nothing ticks otherwise.
// Depends on: CompanionSprite / CompanionImage / CompanionImageFade / CompanionSpeechBubble in
// MainPage.xaml, _companionState and _settings.ReduceMotion.
// Called by: UpdateCompanionDisplay (ApplyCompanionFrame) and UpdateCompanionSpeech
// (ShowCompanionSpeech) in MainPage.Companion.cs.

using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    // One frame of pet motion, relative to the sprite's resting pose (pixels and degrees).
    private readonly record struct CompanionPose(double X, double Y, double Rotation, double ScaleX, double ScaleY)
    {
        public static readonly CompanionPose Rest = new(0, 0, 0, 1, 1);

        public static CompanionPose Lerp(CompanionPose from, CompanionPose to, double amount) => new(
            from.X + (to.X - from.X) * amount,
            from.Y + (to.Y - from.Y) * amount,
            from.Rotation + (to.Rotation - from.Rotation) * amount,
            from.ScaleX + (to.ScaleX - from.ScaleX) * amount,
            from.ScaleY + (to.ScaleY - from.ScaleY) * amount);
    }

    private const double CompanionFrameSeconds = 1.0 / 30;
    private const double CompanionWalkFrameSeconds = 1.0 / 60;
    private const double CompanionFadeSeconds = 0.22;
    private const double CompanionBlendSeconds = 0.35;
    private const double CompanionSpeechFadeSeconds = 0.25;
    private const double CompanionIdleSpeechSeconds = 10;

    private readonly Stopwatch _companionClock = Stopwatch.StartNew();
    private readonly Dictionary<string, ImageSource> _companionFrameCache = new(StringComparer.Ordinal);
    private DispatcherTimer? _companionTimer;
    private DispatcherTimer? _companionIdleReturnTimer;
    private CompositeTransform? _companionTransform;
    private Grid? _companionPreloadLayer;
    private string _companionPreloadedPet = "";
    private bool _companionAnimationHooked;
    private bool _companionWindowVisible = true;
    private string _companionShownState = "idle";
    private string _companionShownPet = "";
    private string _companionBlendFromState = "idle";
    private double _companionStateChangedAt = double.NegativeInfinity;
    private double _companionFadeStartedAt = double.NegativeInfinity;
    private double _companionSpeechTarget = 1;
    private double _companionSpeechFrom = 1;
    private double _companionSpeechFadeStartedAt = double.NegativeInfinity;
    private double _companionSpeechHideAt = double.PositiveInfinity;
    private double _companionLastTickAt;

    private double CompanionNow => _companionClock.Elapsed.TotalSeconds;

    // The app's "Reduce decorative motion" setting, or the system's animation preference.
    private bool CompanionReduceMotion => _settings.ReduceMotion || SystemPrefersReducedMotion();

    private static bool? _systemPrefersReducedMotion;

    // Windows reports "Show animations" here; platforms where Uno has no answer count as "no
    // preference". Read once: the property is cheap but the platform lookup is not.
    private static bool SystemPrefersReducedMotion()
    {
        if (_systemPrefersReducedMotion is { } known) return known;
        try { _systemPrefersReducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled; }
        catch (Exception) { _systemPrefersReducedMotion = false; }
        return _systemPrefersReducedMotion.Value;
    }

    // Wires the transform and the visibility listeners once. Watching the overlay's Visibility
    // covers every way the pet is shown or hidden (sidebar button, ×, Settings, drawer).
    private void EnsureCompanionAnimationHooks()
    {
        if (_companionAnimationHooked) return;
        _companionAnimationHooked = true;
        // The sidebar has ZIndex 2 (so the compact drawer covers its scrim), which hid the pet
        // whenever it was dragged or walked over the sidebar. The overlay is collapsed while the
        // drawer is open (SetCompanionOverlayBlocked), so above the sidebar is always right.
        Canvas.SetZIndex(CompanionOverlay, 3);
        _companionTransform = new CompositeTransform { CenterX = 40, CenterY = 80 };
        CompanionSprite.RenderTransform = _companionTransform;
        _companionSpeechHideAt = CompanionNow + CompanionIdleSpeechSeconds;
        CompanionOverlay.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => RefreshCompanionAnimation());
        if ((Application.Current as App)?.MainWindow is { } window)
        {
            window.VisibilityChanged += (_, args) =>
            {
                _companionWindowVisible = args.Visible;
                RefreshCompanionAnimation();
            };
        }
    }

    private bool CompanionShouldAnimate =>
        _companionAnimationHooked
        && _companionWindowVisible
        && !CompanionReduceMotion
        && CompanionOverlay.Visibility == Visibility.Visible;

    // Starts or stops the single animation timer to match the current conditions.
    private void RefreshCompanionAnimation()
    {
        if (!_companionAnimationHooked) return;
        if (CompanionShouldAnimate)
        {
            if (_companionTimer is null)
            {
                _companionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(CompanionFrameSeconds) };
                _companionTimer.Tick += (_, _) => CompanionTick();
            }
            if (!_companionTimer.IsEnabled) _companionTimer.Start();
            return;
        }
        _companionTimer?.Stop();
        // With Reduce motion the pet rests in its plain pose, and fades finish at once.
        if (CompanionReduceMotion) SettleCompanionVisuals();
    }

    private void SettleCompanionVisuals()
    {
        ApplyCompanionPose(CompanionPose.Rest);
        CompanionImage.Opacity = 1;
        CompanionImageFade.Opacity = 0;
        CompanionImageFade.Source = null;
        CompanionSpeechBubble.Opacity = _companionSpeechTarget;
    }

    // Shows a frame. A change of pet or state cross-fades from the previous frame; a refresh of
    // the same frame (for example after a resize) changes nothing.
    private void ApplyCompanionFrame(ImageSource source)
    {
        EnsureCompanionAnimationHooks();
        source = CachedCompanionFrame(source);
        var state = _companionState.CurrentState;
        var pet = _companionState.PetId;
        var changed = !ReferenceEquals(CompanionImage.Source, source);
        var stateChanged = !state.Equals(_companionShownState, StringComparison.Ordinal);

        if (changed && CompanionImage.Source is not null && CompanionShouldAnimate && _companionShownPet == pet)
        {
            CompanionImageFade.Source = CompanionImage.Source;
            CompanionImageFade.Opacity = 1;
            CompanionImage.Opacity = 0;
            _companionFadeStartedAt = CompanionNow;
        }
        else if (changed)
        {
            CompanionImageFade.Source = null;
            CompanionImageFade.Opacity = 0;
            CompanionImage.Opacity = 1;
        }
        if (changed) CompanionImage.Source = source;

        if (stateChanged)
        {
            _companionBlendFromState = _companionShownState;
            _companionStateChangedAt = CompanionNow;
            _companionShownState = state;
            ScheduleCompanionIdleReturn(state);
            // The old message belongs to the old state; UpdateCompanionSpeech shows the new one.
            HideCompanionSpeech();
        }
        _companionShownPet = pet;
        PreloadCompanionFrames(pet);
        RefreshCompanionAnimation();
    }

    // Decodes all seven built-in frames of the current pet up front. Without this the first
    // switch to a state waited 2–3 s for the picture, so "thinking" often showed only as the
    // reply finished. The images sit invisibly behind the sprite and share the cached sources.
    private void PreloadCompanionFrames(string pet)
    {
        if (pet == _companionPreloadedPet) return;
        _companionPreloadedPet = pet;
        if (_companionPreloadLayer is null)
        {
            _companionPreloadLayer = new Grid { IsHitTestVisible = false, Opacity = 0.01 };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(_companionPreloadLayer, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            CompanionSprite.Children.Insert(0, _companionPreloadLayer);
        }
        _companionPreloadLayer.Children.Clear();
        foreach (var state in CompanionStates)
        {
            var frame = CachedCompanionFrame(new BitmapImage { UriSource = new Uri($"ms-appx:///Assets/Svg/{pet}-{state}.png") });
            _companionPreloadLayer.Children.Add(new Image { Source = frame, Width = 8, Height = 8, Stretch = Stretch.Uniform });
        }
    }

    // Reuses one ImageSource per built-in file, so switching back to a state does not decode the
    // picture again or flash an empty frame while it loads.
    private ImageSource CachedCompanionFrame(ImageSource source)
    {
        var key = source switch
        {
            BitmapImage { UriSource: { } uri } => uri.OriginalString,
            SvgImageSource { UriSource: { } uri } => uri.OriginalString,
            _ => null
        };
        if (key is null) return source;
        if (_companionFrameCache.TryGetValue(key, out var cached)) return cached;
        if (_companionFrameCache.Count > 64) _companionFrameCache.Clear();
        _companionFrameCache[key] = source;
        return source;
    }

    // success, error and stuck are reactions, not modes: after a while the pet relaxes to idle
    // (it used to keep the error face until the next prompt, and even across restarts).
    private void ScheduleCompanionIdleReturn(string state)
    {
        _companionIdleReturnTimer?.Stop();
        var seconds = state switch
        {
            "success" => 6,
            "error" => 10,
            "stuck" => 12,
            _ => 0
        };
        if (seconds == 0) return;
        _companionIdleReturnTimer ??= CreateCompanionIdleReturnTimer();
        _companionIdleReturnTimer.Interval = TimeSpan.FromSeconds(seconds);
        _companionIdleReturnTimer.Start();
    }

    private DispatcherTimer CreateCompanionIdleReturnTimer()
    {
        var timer = new DispatcherTimer();
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_companionState.CurrentState is not ("success" or "error" or "stuck")) return;
            SetCompanionState("idle");
            HideCompanionSpeech();
        };
        return timer;
    }

    // Called after the speech text changes. Messages shown while idle fade out after a while;
    // during thinking/coding the bubble stays until the state moves on.
    private void ShowCompanionSpeech()
    {
        EnsureCompanionAnimationHooks();
        StartCompanionSpeechFade(1);
        _companionSpeechHideAt = _companionState.CurrentState == "idle"
            ? CompanionNow + CompanionIdleSpeechSeconds
            : double.PositiveInfinity;
        if (!CompanionShouldAnimate) CompanionSpeechBubble.Opacity = 1;
    }

    private void HideCompanionSpeech()
    {
        StartCompanionSpeechFade(0);
        _companionSpeechHideAt = double.PositiveInfinity;
        if (!CompanionShouldAnimate) CompanionSpeechBubble.Opacity = 0;
    }

    // Fades are timed from the clock, not counted in ticks: the timer slows down while a reply
    // streams, and a per-tick step left the "Thinking…" bubble stuck half-visible.
    private void StartCompanionSpeechFade(double target)
    {
        if (Math.Abs(target - _companionSpeechTarget) < 0.001) return;
        _companionSpeechFrom = CompanionSpeechBubble.Opacity;
        _companionSpeechTarget = target;
        _companionSpeechFadeStartedAt = CompanionNow;
    }

    private void CompanionTick()
    {
        if (!CompanionShouldAnimate)
        {
            _companionTimer?.Stop();
            return;
        }
        var now = CompanionNow;

        // Motion: blend from the previous state's loop into the new one so a change never snaps.
        var pose = CompanionStatePose(_companionShownState, now);
        var blend = (now - _companionStateChangedAt) / CompanionBlendSeconds;
        if (blend < 1)
            pose = CompanionPose.Lerp(CompanionStatePose(_companionBlendFromState, now), pose, EaseInOut(Math.Max(0, blend)));
        // Wander (MainPage.CompanionWander.cs) moves the card and swaps in the walking motion.
        var walkPose = CompanionWanderTick(now);
        var tickSeconds = Math.Clamp(now - _companionLastTickAt, 0, 0.1);
        _companionLastTickAt = now;
        ApplyCompanionPose(ComposeCompanionPose(pose, walkPose, tickSeconds));
        // 60 fps while walking so the glide is smooth, 30 fps otherwise.
        var interval = TimeSpan.FromSeconds(CompanionIsWalking ? CompanionWalkFrameSeconds : CompanionFrameSeconds);
        if (_companionTimer is { } timer && timer.Interval != interval) timer.Interval = interval;

        // Frame cross-fade.
        var fade = (now - _companionFadeStartedAt) / CompanionFadeSeconds;
        if (fade < 1)
        {
            var amount = EaseInOut(Math.Max(0, fade));
            CompanionImage.Opacity = amount;
            CompanionImageFade.Opacity = 1 - amount;
        }
        else if (CompanionImageFade.Source is not null)
        {
            CompanionImage.Opacity = 1;
            CompanionImageFade.Opacity = 0;
            CompanionImageFade.Source = null;
        }

        // Speech bubble fade in/out.
        if (now >= _companionSpeechHideAt)
        {
            StartCompanionSpeechFade(0);
            _companionSpeechHideAt = double.PositiveInfinity;
        }
        var speech = (now - _companionSpeechFadeStartedAt) / CompanionSpeechFadeSeconds;
        var bubble = speech >= 1
            ? _companionSpeechTarget
            : _companionSpeechFrom + (_companionSpeechTarget - _companionSpeechFrom) * EaseInOut(Math.Max(0, speech));
        if (Math.Abs(CompanionSpeechBubble.Opacity - bubble) > 0.001) CompanionSpeechBubble.Opacity = bubble;
    }

    private void ApplyCompanionPose(CompanionPose pose)
    {
        if (_companionTransform is null) return;
        _companionTransform.TranslateX = pose.X;
        _companionTransform.TranslateY = pose.Y;
        _companionTransform.Rotation = pose.Rotation;
        _companionTransform.ScaleX = pose.ScaleX;
        _companionTransform.ScaleY = pose.ScaleY;
    }

    // The looping motion for each state. Small on purpose: the frames carry the expression,
    // the motion only makes the pet feel alive. The transform pivots at the feet.
    private static CompanionPose CompanionStatePose(string state, double t)
    {
        const double Tau = Math.PI * 2;
        switch (state)
        {
            case "thinking":
            {
                // Slow head-tilt sway with a gentle float.
                var sway = Math.Sin(Tau * t / 2.6);
                return new(0, -1.5 - 1.5 * Math.Sin(Tau * t / 1.3), 4 * sway, 1, 1);
            }
            case "coding":
            {
                // Quick typing bounce.
                var bounce = Math.Abs(Math.Sin(Tau * t / 0.9));
                return new(0, -2.5 * bounce, 0, 1 + 0.015 * bounce, 1 - 0.015 * bounce);
            }
            case "coding_malicious":
            {
                // Nervous jitter with a menacing pulse.
                var pulse = 1 + 0.03 * (0.5 + 0.5 * Math.Sin(Tau * t / 0.8));
                return new(1.4 * Math.Sin(Tau * t * 6.5), 0.6 * Math.Sin(Tau * t * 4.3), 2 * Math.Sin(Tau * t * 3.1), pulse, pulse);
            }
            case "stuck":
            {
                // Slow droop and wobble, a little squashed.
                var wobble = Math.Sin(Tau * t / 4);
                return new(0, 2, 5 * wobble, 1.02, 0.97 + 0.01 * Math.Sin(Tau * t / 2));
            }
            case "success":
            {
                // Happy hops: a parabolic jump, then a squash on landing.
                const double Period = 1.1;
                const double Air = 0.45;
                var phase = t % Period;
                if (phase < Air)
                {
                    var p = phase / Air;
                    return new(0, -9 * 4 * p * (1 - p), 0, 0.98, 1.03);
                }
                var land = (phase - Air) / (Period - Air);
                var squash = land < 0.25 ? Math.Sin(Math.PI * land / 0.25) : 0;
                return new(0, 0, 0, 1 + 0.05 * squash, 1 - 0.06 * squash);
            }
            case "error":
            {
                // A short head-shake, then a pause, repeating.
                const double Period = 1.6;
                var phase = t % Period;
                if (phase > 0.5) return CompanionPose.Rest;
                var decay = 1 - phase / 0.5;
                return new(4 * Math.Sin(Tau * phase * 6) * decay, 0, 2.5 * Math.Sin(Tau * phase * 6) * decay, 1, 1);
            }
            default:
            {
                // idle: slow breathing from the feet.
                var breath = Math.Sin(Tau * t / 3.2);
                return new(0, -1 - breath, 0, 1 - 0.02 * breath, 1 + 0.04 * breath);
            }
        }
    }

    // coding_malicious: shown while the person reviews a Coding Agent command that would run
    // unsandboxed, if "Let the pet ask for approval before risky actions" is on. Before this
    // nothing in the app ever used the state.
    private void CompanionEnterRiskReview()
    {
        if (!_companionState.AskBeforeActions) return;
        SetCompanionState("coding_malicious");
        UpdateCompanionSpeech("Careful · this command runs unsandboxed. Review it first.");
    }

    private void CompanionLeaveRiskReview(bool approved)
    {
        if (_companionState.CurrentState != "coding_malicious") return;
        SetCompanionState("idle");
        UpdateCompanionSpeech(approved ? "Running the approved command…" : "Okay · nothing ran.");
    }

    private static double EaseInOut(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2;
    }
}
