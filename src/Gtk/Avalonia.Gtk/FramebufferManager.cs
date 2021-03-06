﻿using System;
using Avalonia.Controls.Platform.Surfaces;

namespace Avalonia.Gtk
{
    class FramebufferManager : IFramebufferPlatformSurface, IDisposable
    {
        private readonly WindowImplBase _window;
        private SurfaceFramebuffer _fb;

        public FramebufferManager(WindowImplBase window)
        {
            _window = window;
        }

        public void Dispose()
        {
            _fb?.Deallocate();
        }

        public ILockedFramebuffer Lock()
        {
            if(_window.CurrentDrawable == null)
                throw new InvalidOperationException("Window is not in drawing state");

            var drawable = _window.CurrentDrawable;
            var width = (int) _window.ClientSize.Width;
            var height = (int) _window.ClientSize.Height;
            if (_fb == null || _fb.Width != width ||
                _fb.Height != height)
            {
                _fb?.Deallocate();
                _fb = new SurfaceFramebuffer(width, height);
            }
            _fb.SetDrawable(drawable);
            return _fb;
        }
    }
}
