using System;
using Avalonia.Markup.Xaml;

namespace Westermo.GraphX.Controls.Avalonia.Themes.Fluent;

/// <summary>
/// Fluent Design theme for GraphX controls.
/// Include in your Application.Styles to apply Fluent-styled graph controls.
/// </summary>
public class GraphXFluentTheme : global::Avalonia.Styling.Styles
{
    public GraphXFluentTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}