using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Westermo.GraphX.Controls.Controls;

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

            // Ensure the resource assembly is loaded before Avalonia resolves the style URI.
            var graphXBaseUri = new Uri($"avares://{typeof(GraphAreaBase).Assembly.GetName().Name}/");
            application.Styles.Add(new StyleInclude(graphXBaseUri)
            {
                Source = new Uri(graphXBaseUri, "Themes/DefaultStyles.axaml")
            });

            _initialized = true;
        }
    }
}
