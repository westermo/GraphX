using System;
using Avalonia.Markup.Xaml;

namespace Westermo.GraphX.Controls.Avalonia.Themes.Semi;

/// <summary>
/// Semi Design theme for GraphX controls.
/// Include in your Application.Styles to apply Semi-styled graph controls.
/// </summary>
public class GraphXSemiTheme : global::Avalonia.Styling.Styles
{
    public GraphXSemiTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}