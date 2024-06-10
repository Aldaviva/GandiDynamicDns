namespace GandiDynamicDns.Unfucked;

// ReSharper disable InconsistentNaming
public static class Task2 {

    private static readonly TimeSpan MAX_SHORT_DELAY;

    static Task2() {
        // max duration of Task.Delay with .NET 6 and later
        MAX_SHORT_DELAY = TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        try {
            CancellationTokenSource testCts = new();
            _ = Task.Delay(MAX_SHORT_DELAY, testCts.Token);
            testCts.Cancel();
        } catch (ArgumentOutOfRangeException) {
            // max duration of Task.Delay with .NET 5 and earlier
            MAX_SHORT_DELAY = TimeSpan.FromMilliseconds(int.MaxValue);
        }
    }

    /// <summary>
    /// Like <see cref="Task.Delay(int)"/>, except this supports arbitrarily long delays.
    /// </summary>
    /// <param name="duration">how long to wait</param>
    /// <param name="timeProvider">useful for mocking</param>
    /// <param name="cancellationToken">to stop waiting before <paramref name="duration"/> has elapsed</param>
    /// <returns></returns>
    public static Task Delay(TimeSpan duration, TimeProvider? timeProvider = default, CancellationToken cancellationToken = default) {
        timeProvider ??= TimeProvider.System;
        Task result = Task.CompletedTask;

        for (TimeSpan remaining = duration; remaining > TimeSpan.Zero; remaining = remaining.Subtract(MAX_SHORT_DELAY)) {
            TimeSpan shortDelay = remaining > MAX_SHORT_DELAY ? MAX_SHORT_DELAY : remaining;
            result = result.ContinueWith(_ => Task.Delay(shortDelay, timeProvider, cancellationToken), cancellationToken,
                TaskContinuationOptions.LongRunning | TaskContinuationOptions.NotOnCanceled, TaskScheduler.Current).Unwrap();
        }

        return result;
    }

}