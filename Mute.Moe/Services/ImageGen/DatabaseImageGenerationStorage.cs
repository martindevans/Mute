using Mute.Moe.Services.Database;
using System.Text.Json.Serialization;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerationConfigStorage
    : IKeyValueStorage<ImageGenerationConfig>
{
}

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

    public Prompt Prompt()
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
}

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

public class DatabaseImageGenerationStorage
    : SimpleJsonBlobTable<ImageGenerationConfig>, IImageGenerationConfigStorage
{
    public DatabaseImageGenerationStorage(IDatabaseService database)
        : base("ImageGeneration", database)
    {
    }
}