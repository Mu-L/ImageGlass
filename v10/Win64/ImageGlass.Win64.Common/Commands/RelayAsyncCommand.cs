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
using System.Windows.Input;

namespace ImageGlass.Win64.Common;

public sealed partial class RelayAsyncCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool> _canExecute;
    private readonly DispatcherQueue _dispatcher;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public RelayAsyncCommand(Func<object?, Task> execute, Func<object?, bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && _canExecute.Invoke(parameter);
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute.Invoke(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        var canExecuteChanged = CanExecuteChanged;
        if (canExecuteChanged is not null)
        {
            if (_dispatcher is not null)
            {
                _dispatcher.TryEnqueue(() => canExecuteChanged.Invoke(this, EventArgs.Empty));
            }
            else
            {
                canExecuteChanged.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

