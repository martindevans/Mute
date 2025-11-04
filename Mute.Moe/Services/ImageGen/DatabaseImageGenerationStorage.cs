using Mute.Moe.Services.Database;
using System.Text.Json.Serialization;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Store the config setting used to generate an image
/// </summary>
public interface IImageGenerationConfigStorage
    : IKeyValueStorage<ImageGenerationConfig>;

public class ImageGenerationConfig
{
    [JsonPropertyName("pos")] public required string Positive { get; set; }
    [JsonPropertyName("neg")] public required string Negative { get; set; }

    [JsonPropertyName("fpos")] public string? FacePositive { get; set; }
    [JsonPropertyName("fneg")] public string? FaceNegative { get; set; }
    [JsonPropertyName("hpos")] public string? HandPositive { get; set; }
    [JsonPropertyName("hneg")] public string? HandNegative { get; set; }
    [JsonPropertyName("epos")] public string? EyePositive { get; set; }
    [JsonPropertyName("eneg")] public string? EyeNegative { get; set; }

    [JsonPropertyName("iurl")] public required string? ReferenceImageUrl { get; set; }
    [JsonPropertyName("priv")] public required bool IsPrivate { get; set; }
    [JsonPropertyName("type")] public required ImageGenerationType Type { get; set; }
    [JsonPropertyName("bat")] public required int BatchSize { get; set; }

    public Prompt ToPrompt()
    {
        return new Prompt
        {
            Positive = Positive,
            Negative = Negative,
            EyeEnhancementPositive = EyePositive,
            EyeEnhancementNegative = EyeNegative,
            FaceEnhancementPositive = FacePositive,
            FaceEnhancementNegative = FaceNegative,
            HandEnhancementPositive = HandPositive,
            HandEnhancementNegative = HandNegative,
        };
    }

    public static ImageGenerationConfig FromPrompt(Prompt prompt, string? referenceUrl, bool isPrivate, int batchSize, ImageGenerationType type)
    {
        return new ImageGenerationConfig
        {
            Positive = prompt.Positive,
            Negative = prompt.Negative,
            EyePositive = prompt.EyeEnhancementPositive,
            EyeNegative = prompt.EyeEnhancementNegative,
            HandPositive = prompt.HandEnhancementPositive,
            HandNegative = prompt.HandEnhancementNegative,
            FacePositive = prompt.FaceEnhancementPositive,
            FaceNegative = prompt.FaceEnhancementNegative,

            ReferenceImageUrl = referenceUrl,
            IsPrivate = isPrivate,
            Type = type,
            BatchSize = batchSize,
        };
    }
}

/// <summary>
/// Indicates what type of operation was used to generate an image
/// </summary>
public enum ImageGenerationType
{
    /// <summary>
    /// Prompt (plus reference, if not null) will be used to generate an image.
    /// </summary>
    Generate,

    /// <summary>
    /// Reference image will be upscaled.
    /// </summary>
    Upscale,

    /// <summary>
    /// Image will be outpainted (expanded out in all directions)
    /// </summary>
    Outpaint
}

/// <inheritdoc cref="IImageGenerationConfigStorage" />
public class DatabaseImageGenerationStorage(IDatabaseService database)
    : SimpleJsonBlobTable<ImageGenerationConfig>("ImageGeneration", database), IImageGenerationConfigStorage;