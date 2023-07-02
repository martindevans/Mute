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
    [JsonPropertyName("iurl")] public required string? ReferenceImageUrl { get; set; }
    [JsonPropertyName("priv")] public required bool IsPrivate { get; set; }
    [JsonPropertyName("type")] public required ImageGenerationType Type { get; set; }
    [JsonPropertyName("bat")] public required int BatchSize { get; set; }

    //todo: what do we need to store?
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
}

public class DatabaseImageGenerationStorage
    : SimpleJsonBlobTable<ImageGenerationConfig>, IImageGenerationConfigStorage
{
    public DatabaseImageGenerationStorage(IDatabaseService database)
        : base("ImageGeneration", database)
    {
    }
}