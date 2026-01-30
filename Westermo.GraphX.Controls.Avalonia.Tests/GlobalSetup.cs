// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly

using System.Diagnostics;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
using TUnit.Core.Executors;
using TUnit.Core.Interfaces;
using Westermo.GraphX.Controls.Avalonia.Tests;

// Limit parallel test execution to 1 for Avalonia UI tests - they share UI state
[assembly: TestExecutor<AvaloniaThreadTestExecutor>]

namespace Westermo.GraphX.Controls.Avalonia.Tests;

/// <summary>
/// Limits test parallelism to 1 to avoid threading issues with Avalonia controls
/// </summary>
public class SingleThreadParallelLimit : IParallelLimit
{
    public int Limit => 1;
}

public class AvaloniaThreadTestExecutor : ITestExecutor
{
    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        var ts = Stopwatch.GetTimestamp();
        var dispatcher = await GlobalHooks.GetDispatcher();
        TimeSpan internalElapsed = TimeSpan.Zero;
        try
        {
            await dispatcher.InvokeAsync(async () =>
            {
                var internalTs = Stopwatch.GetTimestamp();
                await action();
                internalElapsed = Stopwatch.GetElapsedTime(internalTs);
            }, DispatcherPriority.Send);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(ts);
            await context.OutputWriter.WriteLineAsync($"Execution time: {internalElapsed}, Total time: {elapsed}");
            GlobalHooks.AddTiming(internalElapsed, elapsed);
        }
    }
}

public class GlobalHooks
{
    private static readonly List<(TimeSpan execution, TimeSpan total, TimeSpan waiting)> _timings = [];

    public static void AddTiming(TimeSpan execution, TimeSpan total)
    {
        var waiting = total - execution;
        _timings.Add((execution, total, waiting));
    }

    private static Thread? _thread;
    private static Dispatcher? _dispatcher;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static async ValueTask<Dispatcher> GetDispatcher()
    {
        await _lock.WaitAsync();
        try
        {
            if (_dispatcher is not null) return _dispatcher;
            var initialized = new TaskCompletionSource<Dispatcher>();
            _thread = new Thread(() =>
            {
                var app = AppBuilder.Configure<Application>()
                    .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                    .SetupWithoutStarting();
                
                // Load GraphX control themes so edge pointers and other template parts work
                var graphXStyles = new StyleInclude(new Uri("avares://Westermo.GraphX.Controls.Avalonia/"))
                {
                    Source = new Uri("avares://Westermo.GraphX.Controls.Avalonia/Themes/DefaultStyles.axaml")
                };
                Application.Current!.Styles.Add(graphXStyles);
                
                initialized.TrySetResult(Dispatcher.UIThread);
                // Run the dispatcher - this keeps the UI thread alive
                Dispatcher.UIThread.MainLoop(CancellationToken.None);
            })
            {
                IsBackground = true,
                Name = "Avalonia UI Thread"
            };
            _thread.Start();

            // Wait for initialization to complete
            _dispatcher = await initialized.Task;
            return _dispatcher;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static readonly CancellationTokenSource _token = new();

    [Before(TestSession)]
    public static async ValueTask SetUp()
    {
        _timings.Clear();
        // Ensure Avalonia is initialized before any tests run
        await GetDispatcher();
    }

    [After(TestSession)]
    public static async ValueTask CleanUp()
    {
        // Signal dispatcher to stop
        await _token.CancelAsync();
        await Dispatcher.UIThread.InvokeAsync(() => Dispatcher.UIThread.ExitAllFrames());
        var averageExec = 0.0;
        var averageTotal = 0.0;
        var averageWait = 0.0;
        var stdExec = 0.0;
        var stdTotal = 0.0;
        var stdWait = 0.0;
        foreach (var (exec, total, wait) in _timings)
        {
            averageExec += exec.TotalSeconds;
            averageTotal += total.TotalSeconds;
            averageWait += wait.TotalSeconds;
        }

        if (_timings.Count > 0)
        {
            averageExec /= _timings.Count;
            averageTotal /= _timings.Count;
            averageWait /= _timings.Count;

            foreach (var (exec, total, wait) in _timings)
            {
                stdExec += (exec.TotalSeconds - averageExec) * (exec.TotalSeconds - averageExec);
                stdTotal += (total.TotalSeconds - averageTotal) * (total.TotalSeconds - averageTotal);
                stdWait += (wait.TotalSeconds - averageWait) * (wait.TotalSeconds - averageWait);
            }

            stdExec /= _timings.Count;
            stdTotal /= _timings.Count;
            stdWait /= _timings.Count;
        }

        Console.WriteLine("Test execution timings:");
        Console.WriteLine($"  Average execution time: {averageExec:F4} s (std: {Math.Sqrt(stdExec):F4} s)");
        Console.WriteLine($"  Average total time:     {averageTotal:F4} s (std: {Math.Sqrt(stdTotal):F4} s)");
        Console.WriteLine($"  Average waiting time:   {averageWait:F4} s (std: {Math.Sqrt(stdWait):F4} s)");
    }
}