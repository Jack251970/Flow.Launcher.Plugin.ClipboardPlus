using System;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.ViewModels;

internal class RelayCommand(Action<object?> action) : ICommand
{
    private readonly Action<object?> _action = action;

    public virtual bool CanExecute(object? parameter)
    {
        return true;
    }

#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    public virtual void Execute(object? parameter)
    {
        _action?.Invoke(parameter);
    }
}
