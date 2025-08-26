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
using System.Text.Json.Serialization;

namespace ImageGlass.Common;


[JsonSerializable(typeof(SingleCommand))]
public partial class SingleCommandJsonContext : JsonSerializerContext { }


public partial class SingleCommand : IgReactive
{
    /// <summary>
    /// Executable action, its value can be:
    /// <list type="bullet">
    ///   <item>
    ///   Name of a menu item in Main Menu. For example: <c>MnuPrint</c>
    ///   </item>
    ///   <item>
    ///   Name of an <c>IG_</c> method. For example: <c>IG_Print</c>
    ///   </item>
    ///   <item>
    ///   Path of executable file/command. For example: <c>cmd.exe</c>
    ///   </item>
    /// </list>
    /// </summary>
    public string Executable { get; set; } = "";


    /// <summary>
    /// Arguments to pass to the <see cref="Executable"/> in JSON format.
    /// </summary>
    public string Argument { get; set; } = "";


    /// <summary>
    /// The next command to execute after running <see cref="Executable"/>.
    /// </summary>
    public SingleCommand? NextCommand { get; set; } = null;


    public SingleCommand(string executable, string argument, SingleCommand? nextCommand)
    {
        Executable = executable;
        Argument = argument;
        NextCommand = nextCommand;
    }


    public override string ToString()
    {
        return Executable;
    }

}



