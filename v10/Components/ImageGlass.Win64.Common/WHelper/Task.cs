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
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;

namespace ImageGlass.Win64.Common;

public partial class WHelper
{
    private static DispatcherQueueTimer? _debounceTimer;


    /// <summary>
    /// Takes the last called action, delays the execution after a certain amount of time has passed.
    /// </summary>
    public static void Debounce(int delayMs, Action action)
    {
        Debounce(delayMs, (a) => action(), 0);
    }


    /// <summary>
    /// Takes the last called action, delays the execution after a certain amount of time has passed.
    /// </summary>
    public static void Debounce<T>(int delayMs, Action<T?> action, T? param = default)
    {
        if (_debounceTimer is null)
        {
            var controller = DispatcherQueueController.CreateOnDedicatedThread();
            _debounceTimer = controller.DispatcherQueue.CreateTimer();
        }

        _debounceTimer.Debounce(() =>
        {
            action(param);
        }, TimeSpan.FromMilliseconds(delayMs));
    }

}
