// Here you could define global logic that would affect all tests

// You can use attributes at the assembly level to apply to all tests in the assembly

using Avalonia;
using Avalonia.Headless;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

public class GlobalHooks
{
    private static bool _avaloniaInitialized;

    [Before(TestSession)]
    public static void SetUp()
    {
        // Initialize Avalonia in headless mode for all tests
        if (!_avaloniaInitialized)
        {
            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            _avaloniaInitialized = true;
        }
    }

    [After(TestSession)]
    public static void CleanUp()
    {
        // Cleanup if needed
    }
}