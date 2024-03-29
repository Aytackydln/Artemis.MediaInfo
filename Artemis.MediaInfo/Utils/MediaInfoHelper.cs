﻿using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Artemis.Core.ColorScience;
using SkiaSharp;

namespace Artemis.MediaInfo.Utils;

public static class MediaInfoHelper
{
    internal static async Task<ColorSwatch> ReadMediaColors(IRandomAccessStreamReference thumbnail)
    {
        var imageStream = await thumbnail.OpenReadAsync();
        var fileBytes = new byte[imageStream.Size];

        using (var reader = new DataReader(imageStream))
        {
            await reader.LoadAsync((uint)imageStream.Size);
            reader.ReadBytes(fileBytes);
        }

        using var bitmap = SKBitmap.Decode(fileBytes);
        var skClrs = ColorQuantizer.Quantize(bitmap.Pixels, 256);
        return ColorQuantizer.FindAllColorVariations(skClrs, true);
    }
}