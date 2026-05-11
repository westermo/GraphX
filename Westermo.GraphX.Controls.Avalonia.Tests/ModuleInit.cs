using System.Runtime.CompilerServices;
using VerifyTests;

namespace Westermo.GraphX.Controls.Avalonia.Tests;

internal static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        // Allow small per-OS / Skia-version rendering differences (anti-alias,
        // sub-pixel placement, font metrics) without producing snapshot churn.
        VerifyImageMagick.RegisterComparers(threshold: 0.25);
        VerifyAvalonia.Initialize();
    }
}
