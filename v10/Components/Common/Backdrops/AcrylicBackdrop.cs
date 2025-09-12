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
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ImageGlass.Common;

public sealed partial class AcrylicBackdrop : SystemBackdrop
{
    private Color? _color;
    private float _tintOpacity;
    private float _luminosityOpacity;
    private Color? _fallbackColor;
    internal DesktopAcrylicController? _acrylicController = null;


    public readonly DesktopAcrylicKind Kind;
    public SystemBackdropConfiguration? BackdropConfiguration { get; private set; } = null;


    public Color? TintColor
    {
        get { return _color; }
        set
        {
            _color = value;
            if (_acrylicController != null && value != null)
            {
                _acrylicController.TintColor = (Color)value;
            }
        }
    }


    public float TintOpacity
    {
        get { return _tintOpacity; }
        set
        {
            _tintOpacity = value;
            if (_acrylicController != null)
            {
                _acrylicController.TintOpacity = value;
            }
        }
    }


    public float LuminosityOpacity
    {
        get { return _luminosityOpacity; }
        set
        {
            _luminosityOpacity = value;
            if (_acrylicController != null)
            {
                _acrylicController.LuminosityOpacity = value;
            }
        }
    }


    public Color? FallbackColor
    {
        get { return _fallbackColor; }
        set
        {
            _fallbackColor = value;
            if (_acrylicController != null && value != null)
            {
                _acrylicController.FallbackColor = (Color)value;
            }
        }
    }



    public AcrylicBackdrop() : this(DesktopAcrylicKind.Default) { }

    public AcrylicBackdrop(DesktopAcrylicKind desktopAcrylicKind)
    {
        Kind = desktopAcrylicKind;
    }



    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        _acrylicController = new DesktopAcrylicController() { Kind = this.Kind };
        _acrylicController.AddSystemBackdropTarget(connectedTarget);

        BackdropConfiguration = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
        _acrylicController.SetSystemBackdropConfiguration(BackdropConfiguration);
    }


    protected override void OnDefaultSystemBackdropConfigurationChanged(ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        if (target != null)
            base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
    }


    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);

        if (_acrylicController is not null)
        {
            _acrylicController.RemoveSystemBackdropTarget(disconnectedTarget);
            _acrylicController = null;
        }
    }

}
