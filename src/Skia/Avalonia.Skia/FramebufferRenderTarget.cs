﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class FramebufferRenderTarget : IRenderTarget
    {
        private readonly IFramebufferPlatformSurface _surface;

        public FramebufferRenderTarget(IFramebufferPlatformSurface surface)
        {
            _surface = surface;
        }

        public void Dispose()
        {
            //Nothing to do here, since we don't own framebuffer
        }


        SKColorType TranslatePixelFormat(PixelFormat fmt)
        {
            if(fmt == PixelFormat.Rgb565)
                return SKColorType.Rgb565;
            if(fmt == PixelFormat.Bgra8888)
                return SKColorType.Bgra8888;
            if (fmt == PixelFormat.Rgba8888)
                return SKColorType.Rgba8888;
            throw new ArgumentException("Unknown pixel format: " + fmt);
        }


        class PixelFormatShim : IDisposable
        {
            private readonly SKImageInfo _nfo;
            private readonly IntPtr _fb;
            private readonly int _rowBytes;
            private SKBitmap _bitmap;

            public PixelFormatShim(SKImageInfo nfo, IntPtr fb, int rowBytes)
            {
                _nfo = nfo;
                _fb = fb;
                _rowBytes = rowBytes;

               
                _bitmap = new SKBitmap(nfo.Width, nfo.Height);
                if (!_bitmap.CanCopyTo(nfo.ColorType))
                {
                    _bitmap.Dispose();
                    throw new Exception(
                        $"Unable to create pixel format shim for conversion from {_bitmap.ColorType} to {nfo.ColorType}");
                }
            }

            public SKSurface CreateSurface() => SKSurface.Create(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes);

            public void Dispose()
            {
                using (var tmp = _bitmap.Copy(_nfo.ColorType))
                    tmp.CopyPixelsTo(_fb, _nfo.BytesPerPixel * _nfo.Height * _rowBytes, _rowBytes);
                _bitmap.Dispose();
            }
            
        }

        public DrawingContext CreateDrawingContext()
        {
            var fb = _surface.Lock();
            PixelFormatShim shim = null;
            SKImageInfo framebuffer = new SKImageInfo(fb.Width, fb.Height, TranslatePixelFormat(fb.Format),
                SKAlphaType.Opaque);
            var surface = SKSurface.Create(framebuffer, fb.Address, fb.RowBytes) ??
                          (shim = new PixelFormatShim(framebuffer, fb.Address, fb.RowBytes))
                          .CreateSurface();
            if (surface == null)
                throw new Exception("Unable to create a surface for pixel format " + fb.Format +
                                    " or pixel format translator");
            var canvas = surface.Canvas;
            
            
            
            canvas.RestoreToCount(0);
            canvas.Save();
            canvas.Clear(SKColors.Red);
            canvas.ResetMatrix();
            
            return new DrawingContext(new DrawingContextImpl(canvas, canvas, surface, shim, fb),
                Matrix.CreateScale(fb.Dpi.Width / 96, fb.Dpi.Height / 96));
        }
    }
}
