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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Threading;
using Windows.UI.Composition;
using MsComposition = Microsoft.UI.Composition;

namespace ImageGlass.Common;

public abstract partial class CompositionBrushBackdrop : SystemBackdrop
{
    static Compositor? _compositor;
    static Lock _compositorLock = new Lock();


    internal static Compositor Compositor
    {
        get
        {
            if (_compositor == null)
            {
                lock (_compositorLock)
                {
                    if (_compositor == null)
                    {
                        DispatcherQueue.GetForCurrentThread().EnsureSystemDispatcherQueue();
                        _compositor = new Compositor();
                    }
                }
            }

            return _compositor;
        }
    }


    public CompositionBrushBackdrop()
    {
    }


    protected abstract CompositionBrush CreateBrush(Compositor compositor);


    protected override void OnDefaultSystemBackdropConfigurationChanged(MsComposition.ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
    {
        if (target != null)
            base.OnDefaultSystemBackdropConfigurationChanged(target, xamlRoot);
    }


    protected override void OnTargetConnected(MsComposition.ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        connectedTarget.SystemBackdrop = CreateBrush(Compositor);
        base.OnTargetConnected(connectedTarget, xamlRoot);
    }


    protected override void OnTargetDisconnected(MsComposition.ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        var backdrop = disconnectedTarget.SystemBackdrop;
        disconnectedTarget.SystemBackdrop = null;
        backdrop?.Dispose();
        base.OnTargetDisconnected(disconnectedTarget);
    }


}
