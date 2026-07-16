using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HandyAgentFramework.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SkiaSharp;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Analyses images uses an agent, provides description and suggested title.
/// </summary>
public class AgentImageAnalyser<TModel, TClient>
    : IImageAnalyser
    where TModel : IChatModel
    where TClient : IChatClient
{
    private const string DESC_PROMPT = """
                                       You are an image description endpoint. You describe images concisely and accurately, never return a blank response 
                                       or refuse to describe an image. Keep responses to 3–10 short sentences. Focus mainly on what is visible, but you may 
                                       include interpretation (e.g. explaining visual humour) when it's strongly suggested.
                                       """;

    private const string TITLE_PROMPT = "Suggest a title for this image (1-5 words)";

    private readonly TModel _model;
    /// <inheritdoc />
    public string ModelName => _model.Name;

    private readonly Options _options;

    private readonly AIAgent _agent;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentImageAnalyser{TModel, TClient}"/> class.
    /// </summary>
    /// <param name="model">
    /// The chat model used for image analysis. Must implement <see cref="IChatModel"/>.
    /// </param>
    /// <param name="client">
    /// The chat client used to interact with the AI agent. Must implement <see cref="IChatClient"/>.
    /// </param>
    /// <param name="options">
    /// Optional configuration for prompts and image processing settings. If not provided, default options will be used.
    /// </param>
    public AgentImageAnalyser(TModel model, TClient client, Options? options = null)
    {
        _model = model;
        _options = options ?? new();

        _agent = client
                .AsAIAgent(
                     new ChatClientAgentOptions
                     {
                         ChatOptions = new ChatOptions
                         {
                             Instructions = _options.DescriptionPrompt,
                             Tools = [],
                             ModelId = _model.Name,
                         },
                     }).AsBuilder()
                .Build();
    }

    /// <inheritdoc />
    public async Task<ImageAnalysisResult> GetImageDescription(Stream imageStream, CancellationToken cancellation = default)
    {
        // Transcode the input image stream into a format that's acceptable for LLM upload
        var (uploadStream, mimeType) = await PrepareImageAsync(imageStream, cancellation);

        try
        {
            // Create a session
            var session = await _agent.CreateSessionAsync(cancellation);

            // Get a description
            var responseDesc = await _agent.RunAsync(
                new ChatMessage(
                    ChatRole.User,
                    [
                        new TextContent("Describe this image"),
                        await DataContent.LoadFromAsync(uploadStream, mimeType, cancellation)
                    ]),
                session,
                cancellationToken: cancellation);

            // Get a title
            var responseTitle = await _agent.RunAsync(
                new ChatMessage(ChatRole.User, _options.TitlePrompt),
                session,
                cancellationToken: cancellation);

            // Return result
            return new ImageAnalysisResult(
                responseTitle.Text,
                responseDesc.Text
            );
        }
        finally
        {
            if (!ReferenceEquals(uploadStream, imageStream))
                await uploadStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Given an image stream in some unknown format, convert it to a supported format
    /// </summary>
    /// <param name="imageStream"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async ValueTask<(Stream Stream, string MimeType)> PrepareImageAsync(Stream imageStream, CancellationToken cancellation)
    {
        // Ensure we can rewind the stream.
        if (!imageStream.CanSeek)
        {
            var copy = new MemoryStream();
            await imageStream.CopyToAsync(copy, cancellation);
            copy.Position = 0;
            imageStream = copy;
        }

        // Go to the start
        imageStream.Position = 0;

        // Analyse the stream to work out format
        // (wrapped in managed stream to prevent disposal of input)
        using var codec = SKCodec.Create(new SKManagedStream(imageStream, false))
                       ?? throw new InvalidOperationException("Unsupported or invalid image.");

        // Check if it's a supported format
        var mimeType = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Gif => "image/gif",
            SKEncodedImageFormat.Webp => "image/webp",
            _ => null
        };

        // It was fine, return as-is
        if (mimeType is not null)
        {
            imageStream.Position = 0;
            return (imageStream, mimeType);
        }

        // Decode the image into an in-memory image
        imageStream.Position = 0;
        using var bitmap = SKBitmap.Decode(imageStream)
                        ?? throw new InvalidOperationException("Failed to decode image.");

        // Encode it to JPEG, a known-good format
        var jpeg = new MemoryStream();
        if (!bitmap.Encode(jpeg, SKEncodedImageFormat.Jpeg, _options.JpegQuality))
            throw new InvalidOperationException("Failed to encode image");

        // Return the JPEG image
        jpeg.Position = 0;
        return (jpeg, "image/jpeg");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="DescriptionPrompt">System prompt for image analysis</param>
    /// <param name="TitlePrompt">Input message requesting a title</param>
    /// <param name="JpegQuality">Quality of the image when converted to JPEG</param>
    public record Options(
        string DescriptionPrompt = DESC_PROMPT,
        string TitlePrompt = TITLE_PROMPT,
        int JpegQuality = 55
    );
}