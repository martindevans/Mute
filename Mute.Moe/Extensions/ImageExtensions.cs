using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using System.Security.Cryptography;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace Mute.Moe.Extensions;

public static class ImageExtensions
{
    public static string? GetGenerationMetadata(this PngMetadata pngMeta)
    {
        var parameters = pngMeta.TextData.FirstOrDefault(a => a.Keyword == "parameters");

        if (string.IsNullOrEmpty(parameters.Value))
            return null;

        return parameters.Value;
    }


    public static (string, string)? GetGenerationPrompt(this Image image)
    {
        return image.Metadata.GetGenerationPrompt();
    }

    public static (string, string)? GetGenerationPrompt(this ImageMetadata meta)
    {
        var pngMeta = meta.GetPngMetadata();
        return pngMeta.GetGenerationPrompt();
    }

    public static (string, string)? GetGenerationPrompt(this PngMetadata pngMeta)
    {
        var parameters = GetGenerationMetadata(pngMeta);
        if (string.IsNullOrEmpty(parameters))
            return null;

        var lines = parameters.Split('\n');

        const string npHeader = "Negative prompt: ";
        if (lines[0].StartsWith(npHeader))
            return ("", lines[0]);

        var positive = lines[0];
        var negative = lines[1];

        if (!negative.StartsWith(npHeader))
            return null;
        negative = negative.Remove(0, npHeader.Length);

        return (positive, negative);
    }

    public static void Bleed(this Image<Rgba32> image, Rectangle from, int radius, int? seed, float distanceRandFactor)
    {
        var rng = new Random(seed ?? RandomNumberGenerator.GetInt32(int.MaxValue));

        for (var i = 0; i < image.Width; i++)
        {
            for (var j = 0; j < image.Height; j++)
            {
                if (from.Contains(i, j))
                    continue;

                var pixel = image[i, j].ToVector4();

                // Get the distance to the closest point on the rectangle
                var actualClosest = new Point
                {
                    X = Math.Clamp(i, from.Left, from.Right),
                    Y = Math.Clamp(j, from.Top, from.Bottom)
                };
                var distance = Vector2.Distance(new Vector2(i, j), new Vector2(actualClosest.X, actualClosest.Y));

                // Set another point (still on the rectangle border) randomly offset, more randomness with distance
                var offsetClosest = new Point
                {
                    X = Math.Clamp(i + (int)Math.Round((rng.NextSingle() * 2 - 1) * distance * distanceRandFactor), from.Left, from.Right),
                    Y = Math.Clamp(j + (int)Math.Round((rng.NextSingle() * 2 - 1) * distance * distanceRandFactor), from.Top, from.Bottom)
                };
                var closestPixel = image[offsetClosest.X, offsetClosest.Y].ToVector4();

                if (distance < radius)
                    image[i, j] = new Rgba32(Vector4.Lerp(closestPixel, pixel, MathF.Pow(distance / radius, 1)));
            }
        }
    }

    public static void EdgeNoise(this Image<Rgba32> image, int noiseWidth)
    {
        var rng = new Random(RandomNumberGenerator.GetInt32(int.MaxValue));
        Span<byte> bytes = stackalloc byte[3];

        for (var i = 0; i < image.Width; i++)
        {
            for (var j = 0; j < image.Height; j++)
            {
                var distance = Distance(image.Bounds, new Point(i, j)) / noiseWidth;
                if (distance >= 1)
                    continue;

                if (rng.NextSingle() < distance)
                    continue;

                rng.NextBytes(bytes);
                var pixel = new Rgba32(bytes[0], bytes[1], bytes[2], 255);

                var original = image[i, j];
                var blend = new Rgba32(original.ToVector4() * distance + pixel.ToVector4() * (1 - distance))
                {
                    A = original.A,
                };
                image[i, j] = blend;
            }
        }

        return;

        static float Distance(Rectangle rect, Point point)
        {
            if (!rect.Contains(point))
                return 0;

            if (point.X > 700 && point.Y > 700)
            {

            }

            var x = Math.Min(point.X - rect.Left, rect.Right - point.X);
            var y = Math.Min(point.Y - rect.Top, rect.Bottom - point.Y);
            return Math.Min(x, y);
        }
    }
}