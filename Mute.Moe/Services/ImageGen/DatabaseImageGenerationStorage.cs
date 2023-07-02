using Mute.Moe.Services.Database;
using System.Text.Json.Serialization;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerationConfigStorage
    : IKeyValueStorage<ImageGenerationData>
{
}

public class ImageGenerationData
{
    [JsonPropertyName("pos")] public required string Positive { get; set; }
    [JsonPropertyName("neg")] public required string Negative { get; set; }
    [JsonPropertyName("iurl")] public required string? ReferenceImage { get; set; }

    //todo: what do we need to store?
}

public class DatabaseImageGenerationStorage
    : SimpleJsonBlobTable<ImageGenerationData>
{
    public DatabaseImageGenerationStorage(IDatabaseService database)
        : base("ImageGeneration", database)
    {
    }
}