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
using ImageGlass.Win64.Common;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace ImageGlass.Win64.UI;

public partial class IgToolbarItemSeparator : UserControl, IIgToolbarItem
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
    public void ClearPropertyChangedEvents()
    {
        // remove PropertyChanged events
        foreach (var eventHandler in _propertyChangedEvent)
        {
            _propertyChangedHandler -= eventHandler;
        }
        _propertyChangedEvent.Clear();
    }

    #endregion // INotifyPropertyChanged Implementation


    protected ToolbarItemModel _vm = new();


    // Public Properties
    #region Public Properties

    /// <summary>
    /// Gets, sets view model for the control.
    /// </summary>
    public ToolbarItemModel VM
    {
        get => _vm;
        set
        {
            if (_vm != value)
            {
                _vm = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion // Public Properties


    public IgToolbarItemSeparator()
    {
        InitializeComponent();

        Unloaded += IgToolbarItemSeparator_Unloaded;
    }


    private void IgToolbarItemSeparator_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Unloaded -= IgToolbarItemSeparator_Unloaded;
        ClearPropertyChangedEvents();
    }

}
