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
using System;

namespace ImageGlass.Win64.UI;


public enum DialogExitCode
{
    /// <summary>
    /// Nothing is returned from the dialog box. This means that the modal dialog continues running.
    /// </summary>
    None = 0,
    /// <summary>
    /// The dialog box return value is OK (usually sent from a button labeled OK).
    /// </summary>
    OK = 1,
    /// <summary>
    /// The dialog box return value is Cancel (usually sent from a button labeled Cancel).
    /// </summary>
    Cancel = 2,
    /// <summary>
    /// The dialog box return value is Abort (usually sent from a button labeled Abort).
    /// </summary>
    Abort = 3,
}


public enum DialogButton
{
    Button1,
    Button2,
    Button3,
}


public enum DialogAction
{
    Submit,
    Cancel,
    Apply,
}


public enum DialogFocus
{
    Default,
    Button1,
    Button2,
    Button3,
}


/// <summary>
/// The built-in buttons for <see cref="PopupWindow"/>.
/// </summary>
public enum PopupButton
{
    OK,
    Close,
    Yes_No,
    OK_Cancel,
    OK_Close,
    LearnMore_Close,
    Continue_Quit,
}


/// <summary>
/// Specifies identifiers to indicate the return data of a dialog.
/// </summary>
public class PopupResult
{
    /// <summary>
    /// Gets the exit result of the dialog.
    /// </summary>
    public DialogExitCode ExitCode { get; internal set; } = DialogExitCode.None;

    /// <summary>
    /// Gets the value of input.
    /// </summary>
    public string InputValue { get; internal set; } = "";

    /// <summary>
    /// Gets the check state of the Remember checkbox option.
    /// </summary>
    public bool IsRememberOptionChecked { get; internal set; } = false;
}


public class DialogEventArgs(DialogAction actionType) : EventArgs
{
    public DialogAction ActionType => actionType;
    public bool CanProceed { get; set; } = true;
}
