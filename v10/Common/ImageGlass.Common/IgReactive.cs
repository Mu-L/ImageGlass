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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageGlass.Common;


/// <summary>
/// Provides a base implementation of <see cref="INotifyPropertyChanged"/> interface.
/// </summary>
public class IgReactive : INotifyPropertyChanged
{
    #region INotifyPropertyChanged Implementation

    // to manage PropertyChanged events
    private List<PropertyChangedEventHandler> _propertyChangedEvent = new();
    private event PropertyChangedEventHandler? _propertyChangedHandler;


    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            if (value != null)
            {
                _propertyChangedHandler += value;
                _propertyChangedEvent.Add(value);
            }
        }

        remove
        {
            if (value != null)
            {
                _propertyChangedHandler -= value;
                _propertyChangedEvent.Remove(value);
            }
        }
    }


    /// <summary>
    /// Emits event <see cref="PropertyChanged"/>.
    /// </summary>
    public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        _propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    /// <summary>
    /// Clears event handlers list of <see cref="PropertyChanged"/>.
    /// </summary>
    public void CleanUpPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvent)
        {
            _propertyChangedHandler -= eventHandler;
        }
        _propertyChangedEvent.Clear();
    }

    #endregion // INotifyPropertyChanged Implementation

}
