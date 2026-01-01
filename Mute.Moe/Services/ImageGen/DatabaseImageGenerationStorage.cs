using Mute.Moe.Services.Database;
using System.Text.Json.Serialization;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Store the config settings used to generate an image
/// </summary>
public interface IImageGenerationConfigStorage
    : IKeyValueStorage<ImageGenerationConfig>;

/// <summary>
/// The config settings used to generate an image
/// </summary>
public class ImageGenerationConfig
{
    /// <summary>
    /// Positive prompt
    /// </summary>
    [JsonPropertyName("pos")] public required string Positive { get; set; }

    /// <summary>
    /// Negative prompt
    /// </summary>
    [JsonPropertyName("neg")] public required string Negative { get; set; }

    /// <summary>
    /// Positive prompt for face
    /// </summary>
    [JsonPropertyName("fpos")] public string? FacePositive { get; set; }

    /// <summary>
    /// Negative prompt for face
    /// </summary>
    [JsonPropertyName("fneg")] public string? FaceNegative { get; set; }

    /// <summary>
    /// Positive prompt for hands
    /// </summary>
    [JsonPropertyName("hpos")] public string? HandPositive { get; set; }

    /// <summary>
    /// Negative prompt for hands
    /// </summary>
    [JsonPropertyName("hneg")] public string? HandNegative { get; set; }

    /// <summary>
    /// Positive prompt for eyes
    /// </summary>
    [JsonPropertyName("epos")] public string? EyePositive { get; set; }

    /// <summary>
    /// Negative prompt for eyes
    /// </summary>
    [JsonPropertyName("eneg")] public string? EyeNegative { get; set; }

    /// <summary>
    /// Url for reference image, if this is img2img
    /// </summary>
    [JsonPropertyName("iurl")] public required string? ReferenceImageUrl { get; set; }

    /// <summary>
    /// Indicates if this image was generated in a private context
    /// </summary>
    [JsonPropertyName("priv")] public required bool IsPrivate { get; set; }

    /// <summary>
    /// Type of image generation
    /// </summary>
    [JsonPropertyName("type")] public required ImageGenerationType Type { get; set; }

    /// <summary>
    /// How many images to generate in the batch
    /// </summary>
    [JsonPropertyName("bat")] public required int BatchSize { get; set; }

    /// <summary>
    /// Convert to a prompt
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Convert from a prompt
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="referenceUrl"></param>
    /// <param name="isPrivate"></param>
    /// <param name="batchSize"></param>
    /// <param name="type"></param>
    /// <returns></returns>
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