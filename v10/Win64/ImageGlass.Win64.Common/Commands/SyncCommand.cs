/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2025 DUONG DIEU PHAP
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
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace ImageGlass.Win64.Common;

public sealed partial class SyncCommand : IIgCommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;
    private readonly DispatcherQueue _dispatcher;

    public event EventHandler? CanExecuteChanged;
    public bool IsAsync => false;


    public SyncCommand(Action<object?> execute, Func<object?, bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute.Invoke(parameter);
    }

    public void Execute(object? parameter)
    {
        _execute.Invoke(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        await Task.Delay(0);
        Execute(parameter);
    }


    public void RaiseCanExecuteChanged()
    {
        if (_dispatcher is not null)
        {
            _dispatcher.TryEnqueue(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
        else
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
