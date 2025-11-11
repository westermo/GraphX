using System;
using System.Windows.Input;

namespace ShowcaseApp.Avalonia.Models;

public class SimpleCommand(Predicate<object?> can, Action<object?> ex) : ICommand
{
    public Predicate<object?> CanExecuteDelegate { get; set; } = can;
    public Action<object?> ExecuteDelegate { get; set; } = ex;

    public bool CanExecute(object? parameter)
    {
        return CanExecuteDelegate?.Invoke(parameter) ?? true;
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public void Execute(object? parameter)
    {
        ExecuteDelegate?.Invoke(parameter);
    }
}