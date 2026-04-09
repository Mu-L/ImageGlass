/*
ImageGlass - A lightweight, versatile image viewer
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
namespace ImageGlass.SDK;

/// <summary>
/// ALL_CAP_SNAKE_CASE constants for the IPC message protocol.
/// </summary>
public static class MessageTypes
{
    // Host -> Plugin events
    public const string INIT = "INIT";
    public const string PHOTO_CHANGED = "PHOTO_CHANGED";
    public const string THEME_CHANGED = "THEME_CHANGED";
    public const string LANGUAGE_CHANGED = "LANGUAGE_CHANGED";
    public const string COLOR_PROFILE_CHANGED = "COLOR_PROFILE_CHANGED";
    public const string POINTER_MOVED = "POINTER_MOVED";
    public const string POINTER_PRESSED = "POINTER_PRESSED";
    public const string SELECTION_CHANGED = "SELECTION_CHANGED";
    public const string FRAME_CHANGED = "FRAME_CHANGED";
    public const string SHUTDOWN = "SHUTDOWN";
    public const string EXECUTE = "EXECUTE";

    // Plugin -> Host requests / events
    public const string READ_PIXEL = "READ_PIXEL";
    public const string GET_PIXEL_BUFFER = "GET_PIXEL_BUFFER";
    public const string RELEASE_PIXEL_BUFFER = "RELEASE_PIXEL_BUFFER";
    public const string GET_SOURCE_SIZE = "GET_SOURCE_SIZE";
    public const string GET_SELECTION = "GET_SELECTION";
    public const string SET_SELECTION = "SET_SELECTION";
    public const string ENABLE_SELECTION = "ENABLE_SELECTION";
    public const string SUBSCRIBE_EVENTS = "SUBSCRIBE_EVENTS";
    public const string RUN_API = "RUN_API";
    public const string GET_PHOTO_METADATA = "GET_PHOTO_METADATA";
    public const string GET_PHOTO_LIST = "GET_PHOTO_LIST";
    public const string GET_THEME_INFO = "GET_THEME_INFO";
}
