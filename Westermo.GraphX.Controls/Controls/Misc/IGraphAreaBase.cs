﻿using System.Windows;

namespace Westermo.GraphX.Controls
{
    /// <summary>
    /// Common GraphArea interface
    /// </summary>
    public interface IGraphAreaBase
    {
        void SetPrintMode(bool value, bool offsetControls = true, int margin = 0);

        Rect ContentSize { get; }
    }
}
