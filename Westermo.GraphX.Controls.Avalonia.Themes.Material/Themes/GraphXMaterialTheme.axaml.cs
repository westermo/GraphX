using System;
using Avalonia.Markup.Xaml;

namespace Westermo.GraphX.Controls.Avalonia.Themes.Material;

/// <summary>
/// Material Design theme for GraphX controls.
/// Include in your Application.Styles to apply Material-styled graph controls.
/// </summary>
public class GraphXMaterialTheme : global::Avalonia.Styling.Styles
{
    public GraphXMaterialTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}