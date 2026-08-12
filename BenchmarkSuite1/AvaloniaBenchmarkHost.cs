using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;

namespace GraphXBenchmarks;

internal static class AvaloniaBenchmarkHost
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        lock (InitLock)
        {
            if (_initialized) return;

            AppBuilder.Configure<Application>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = false
                })
                .SetupWithoutStarting();

            var application = Application.Current
                              ?? throw new InvalidOperationException("Avalonia application was not initialized.");

            application.Styles.Add(new StyleInclude(new Uri("avares://Westermo.GraphX.Controls.Avalonia/"))
            {
                Source = new Uri("avares://Westermo.GraphX.Controls.Avalonia/Themes/DefaultStyles.axaml")
            });

            _initialized = true;
        }
    }
}
