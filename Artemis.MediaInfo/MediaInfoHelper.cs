using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Artemis.Core.ColorScience;
using SkiaSharp;

namespace Artemis.MediaInfo;

public static class MediaInfoHelper
{
    internal static async Task<ColorSwatch> ReadMediaColors(GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
    {
        var imageStream = await mediaProperties.Thumbnail.OpenReadAsync();
        var fileBytes = new byte[imageStream.Size];

        using (var reader = new DataReader(imageStream))
        {
            await reader.LoadAsync((uint)imageStream.Size);
            reader.ReadBytes(fileBytes);
        }

        using SKBitmap bitmap = SKBitmap.Decode(fileBytes);
        SKColor[] skClrs = ColorQuantizer.Quantize(bitmap.Pixels, 256);
        return ColorQuantizer.FindAllColorVariations(skClrs, true);
    }
}