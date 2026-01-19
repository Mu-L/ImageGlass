/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace ImageGlass.Common.Commands;

public sealed partial class SyncCommand : IIgCommand
{
    private readonly Action<string?> _executeFn;
    private readonly Func<object?, bool> _canExecuteFn;

    public event EventHandler? CanExecuteChanged;
    public bool IsAsync => false;


    public SyncCommand(Action<string?> execute, Func<object?, bool> canExecute)
    {
        _executeFn = execute;
        _canExecuteFn = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecuteFn.Invoke(parameter);
    }

    public void Execute(object? parameter)
    {
        _executeFn.Invoke(parameter?.ToString());
    }

    public void Execute(string? parameter)
    {
        _executeFn.Invoke(parameter);
    }

    public async Task ExecuteAsync(string? parameter)
    {
        await Task.Delay(0);
        Execute(parameter);
    }


    public void RaiseCanExecuteChanged()
    {
        if (CanExecuteChanged is not null)
        {
            Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}
