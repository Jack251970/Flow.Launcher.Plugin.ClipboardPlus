﻿using System;
using System.Windows.Input;

namespace ClipboardPlus.Panels.ViewModels;

internal class RelayCommand : ICommand
{
    private readonly Action<object?> _action;

    public RelayCommand(Action<object?> action)
    {
        _action = action;
    }

    public virtual bool CanExecute(object? parameter)
    {
        return true;
    }

    public event EventHandler? CanExecuteChanged;

    public virtual void Execute(object? parameter)
    {
        _action?.Invoke(parameter);
    }
}
