using Aioros.Pictor.Demo;
using Aioros.Pictor.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace Aioros.Pictor.Tests.Desktop;

public sealed class BrushHoverTests(DesktopSession desktop)
{
    [Fact]
    [Trait("AC", "BRUSH-11")]
    public async Task TheBrushTextsShowOnHoverAndHideAfterTheLeaveTimeOutAsync()
    {
        var example = await Reference.ExampleAsync("BrushWindowBarChart");

        var states = await desktop.Session.Dispatch(
            () =>
            {
                var clock = new TimerClock();
                var chart = SpecControls.Create(example);
                chart.Time = clock;
                var window = new Window { Width = 500, Height = 300, Content = chart };
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var brush = ((ChartLayout)chart.Plot()).Brush!;
                var traveller = new Point(brush.StartX + 2, brush.Y + (brush.Height / 2));
                var outside = new Point(250, 100);
                bool Shown()
                {
                    Dispatcher.UIThread.RunJobs();
                    return ((ChartLayout)chart.Plot()).Brush!.Texts is not null;
                }

                var rest = Shown();
                window.MouseMove(traveller, RawInputModifiers.None);
                var entered = Shown();
                window.MouseMove(outside, RawInputModifiers.None);
                clock.Advance(999);
                var waiting = Shown();
                window.MouseMove(traveller, RawInputModifiers.None);
                window.MouseMove(outside, RawInputModifiers.None);
                clock.Advance(999);
                var renewed = Shown();
                clock.Advance(1);
                var hidden = Shown();
                window.Close();
                return (rest, entered, waiting, renewed, hidden);
            },
            TestContext.Current.CancellationToken);

        Assert.Equal((false, true, true, true, false), states);
    }

    private sealed class TimerClock : TimeProvider
    {
        private readonly List<Timer> _timers = [];
        private long _ticks = 1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(this, callback, state, _ticks + dueTime.Ticks);
            _timers.Add(timer);
            return timer;
        }

        public void Advance(double milliseconds)
        {
            _ticks += (long)(milliseconds * TimeSpan.TicksPerMillisecond);
            foreach (var timer in _timers.Where(timer => timer.Due <= _ticks).ToList())
            {
                _timers.Remove(timer);
                timer.Fire();
            }
        }

        private void Remove(Timer timer) => _timers.Remove(timer);

        private sealed class Timer(TimerClock clock, TimerCallback callback, object? state, long due) : ITimer
        {
            public long Due => due;

            public void Fire() => callback(state);

            public bool Change(TimeSpan dueTime, TimeSpan period) => false;

            public void Dispose() => clock.Remove(this);

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
