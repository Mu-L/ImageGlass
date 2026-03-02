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
using System.Threading;

namespace ImageGlass.Common;


// Source:
// https://github.com/jiripolasek/PowerToys/blob/3bfa0a0cf8f98a6b5d8c753331c0b35dc3f2a41a/src/modules/cmdpal/Core/Microsoft.CmdPal.Core.Common/Helpers/InterlockedBoolean.cs

/// <summary>
/// Thread-safe boolean implementation using atomic operations.
/// </summary>
public struct InterlockedBool(bool initialValue = false)
{
    private int _value = initialValue ? 1 : 0;


    /// <summary>
    /// Gets or sets the boolean value atomically
    /// </summary>
    public bool Value
    {
        get => Volatile.Read(ref _value) == 1;
        set => Interlocked.Exchange(ref _value, value ? 1 : 0);
    }


    /// <summary>
    /// Atomically sets the value to true
    /// </summary>
    /// <returns>True if the value was previously false, false if it was already true</returns>
    public bool Set()
    {
        return Interlocked.Exchange(ref _value, 1) == 0;
    }


    /// <summary>
    /// Atomically sets the value to false
    /// </summary>
    /// <returns>True if the value was previously true, false if it was already false</returns>
    public bool Clear()
    {
        return Interlocked.Exchange(ref _value, 0) == 1;
    }


    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();
}
