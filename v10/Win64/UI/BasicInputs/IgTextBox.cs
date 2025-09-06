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
using ImageGlass.Common;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.System;

namespace ImageGlass.Win64.UI;

public partial class IgTextBox : TextBox, INotifyPropertyChanged
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


    #region Private Regex
    private static Regex Regex_IntValueOnly => new Regex(
            pattern: $"^[{Const.SIGN_POSITIVE}{Const.SIGN_NEGATIVE}]?[0-9]+$",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static Regex Regex_FloatValueOnly => new Regex(
            pattern: $"^([0-9]+([{Const.DECIMAL_SEPARATOR}][0-9]*)?|[{Const.DECIMAL_SEPARATOR}][0-9]+)$",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static Regex Regex_UnsignedFloatValueOnly => new Regex(
            pattern: $"^[{Const.SIGN_POSITIVE}{Const.SIGN_NEGATIVE}]?([0-9]+([{Const.DECIMAL_SEPARATOR}][0-9]*)?|[{Const.DECIMAL_SEPARATOR}][0-9]+)$",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase);


    [GeneratedRegex("^[0-9]+$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex Regex_UnsignedIntValueOnly();


    #endregion // Private Regex


    public event TypedEventHandler<IgTextBox, ValidatedEventArgs>? Validated;
    private readonly IgTextBox_Description _descriptionEl = new();


    #region Public Properties

    /// <summary>
    /// Gets, sets the message.
    /// </summary>
    public string? Message
    {
        get => _descriptionEl.Message;
        set
        {
            if (_descriptionEl.Message != value)
            {
                _descriptionEl.Message = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the regex pattern for the validation
    /// if <see cref="AcceptValue"/> is <see cref="TextBoxAcceptValue.RegexPattern"/>.
    /// </summary>
    public string? RegexPattern
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// Gets, sets the accepted value of textbox.
    /// </summary>
    public TextBoxAcceptValue AcceptValue
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = TextBoxAcceptValue.Any;


    /// <summary>
    /// Gets, sets the value indicates that empty value is not allowed.
    /// </summary>
    public bool IsRequired
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = false;


    /// <summary>
    /// Gets, sets the value indicates that
    /// the textbox should be validated when pressing Enter key
    /// (and when <see cref="TextBox.AcceptsReturn"/> is <c>false</c>).
    /// </summary>
    public bool ValidateByPressingEnter
    {
        get; set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = true;

    #endregion // Public Properties



    public IgTextBox()
    {
        Description = _descriptionEl;
        Grid.SetColumnSpan((FrameworkElement)Description, 2);

        IsColorFontEnabled = true;
        CornerRadius = (CornerRadius)Application.Current.Resources["ControlCornerRadius"];

        Unloaded += IgTextBox_Unloaded;
        TextChanged += IgTextBox_TextChanged;
    }



    #region Control Events

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // remove delete button
        if (GetTemplateChild("DeleteButton") is Button btnDelete)
        {
            if (btnDelete.Parent is Grid btnDeleteParent)
            {
                btnDeleteParent.Children.Remove(btnDelete);
            }
        }
    }


    private void IgTextBox_Unloaded(object sender, RoutedEventArgs e)
    {
        Unloaded -= IgTextBox_Unloaded;
        TextChanged -= IgTextBox_TextChanged;
    }


    private void IgTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Validate();
    }


    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        if (AcceptsReturn) return;

        // submit the textbox
        if (ValidateByPressingEnter && e.Key == VirtualKey.Enter)
        {
            var isValid = Validate();
            if (!isValid) AnimateErrorIcon();
        }
    }


    /// <summary>
    /// Raises event <see cref="Validated"/>.
    /// </summary>
    protected virtual void OnValidated(ValidatedEventArgs e)
    {
        Validated?.Invoke(this, e);

        _descriptionEl.IsErrorIconVisible = !e.IsValid;
    }

    #endregion // Control Events



    #region Public Methods

    /// <summary>
    /// Validates the input and shows error.
    /// </summary>
    public bool Validate()
    {
        var isValid = true;
        var textValue = Text;
        var isEmpty = string.IsNullOrEmpty(textValue);

        if (isEmpty)
        {
            isValid = !IsRequired;
        }
        else
        {
            // validate according to the accept value
            switch (AcceptValue)
            {
                // use custom regex
                case TextBoxAcceptValue.RegexPattern:
                    if (!string.IsNullOrEmpty(RegexPattern))
                    {
                        isValid = Regex.IsMatch(textValue, RegexPattern);
                    }
                    break;

                // Int value only
                case TextBoxAcceptValue.IntValueOnly:
                    isValid = Regex_IntValueOnly.IsMatch(textValue);
                    break;

                // Unsigned Int value only
                case TextBoxAcceptValue.UnsignedIntValueOnly:
                    isValid = Regex_UnsignedIntValueOnly().IsMatch(textValue);
                    break;

                // Float value only
                case TextBoxAcceptValue.FloatValueOnly:
                    isValid = Regex_FloatValueOnly.IsMatch(textValue);
                    break;

                // Unsigned Float value only
                case TextBoxAcceptValue.UnsignedFloatValueOnly:
                    isValid = Regex_UnsignedFloatValueOnly.IsMatch(textValue);
                    break;

                // Valid filename only
                case TextBoxAcceptValue.FileNameValueOnly:
                    var badChars = Path.GetInvalidFileNameChars();

                    foreach (var c in badChars.AsSpan())
                    {
                        if (textValue.Contains(c))
                        {
                            isValid = false;
                            break;
                        }
                    }
                    break;

                // Any value
                default:
                    break;
            }
        }


        // raise event
        var args = new ValidatedEventArgs(isValid, textValue);
        OnValidated(args);

        return isValid;
    }


    /// <summary>
    /// Animates the error icon.
    /// </summary>
    public void AnimateErrorIcon()
    {
        _descriptionEl.AnimateErrorIcon();
    }

    #endregion // Public Methods


}

