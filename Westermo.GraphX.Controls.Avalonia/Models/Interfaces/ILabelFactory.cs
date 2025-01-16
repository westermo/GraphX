using System.Collections.Generic;
using Avalonia.Controls;

namespace Westermo.GraphX.Controls.Avalonia.Models
{
    /// <summary>
    /// Generic label factory interface. TResult should be at least Control to be able to be added as the GraphArea child.
    /// </summary>
    public interface ILabelFactory<out TResult>
        where TResult : Control
    {
        /// <summary>
        /// Returns newly generated label for parent control. Attachable labels will be auto attached if derived from IAttachableControl
        /// </summary>
        /// <param name="control">Parent control</param>
        IEnumerable<TResult> CreateLabel<TCtrl>(TCtrl control);
    }
}