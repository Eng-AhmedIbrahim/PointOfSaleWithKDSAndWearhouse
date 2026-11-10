using System;
using System.Collections.Generic;
using POS.Contract.Utilities;
using SkiaSharp;

namespace POS.Reports.BarcodeServices;

public static class BarcodeImageHelper
{
    /// <summary>
    /// Renders a Code 128 barcode into crisp PNG image bytes using SkiaSharp.
    /// Pure monochrome, no antialiasing, with full ISO quiet zone for 100% UPOS scanner readability.
    /// </summary>
    public static byte[] GenerateBarcodePng(string text, int height = 60, int moduleWidth = 2, int quietZoneModules = 10)
    {
        var modules = Code128BarcodeGenerator.GenerateModules(text, Math.Max(8, quietZoneModules));
        int mw = Math.Max(1, moduleWidth);
        int totalWidth = modules.Count * mw;
        if (totalWidth <= 0) totalWidth = 100;
        if (height <= 0) height = 40;

        using var bitmap = new SKBitmap(totalWidth, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = false, // Critical: solid black bars, zero dithering for UPOS optical scanners
            FilterQuality = SKFilterQuality.None
        };

        int currentX = 0;
        int barLength = 0;

        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i])
            {
                barLength++;
            }
            else
            {
                if (barLength > 0)
                {
                    int w = barLength * mw;
                    canvas.DrawRect(currentX, 0, w, height, paint);
                    currentX += w;
                    barLength = 0;
                }
                currentX += mw;
            }
        }

        if (barLength > 0)
        {
            int w = barLength * mw;
            canvas.DrawRect(currentX, 0, w, height, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
