using LlmTornado;
using LlmTornado.Chat;
using System.IO;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen
{
    /// <summary>
    /// Analyse images and provide a description of them using vision language models
    /// </summary>
    public class TornadoAnalyser
        : IImageAnalyser
    {
        private readonly TornadoApi _api;
        private readonly Model _model;

        string IImageAnalyser.ModelName => _model.ChatModel.Name;
        bool IImageAnalyser.IsLocal => _model.IsLocal;

        /// <summary>
        /// Create new image analyser, describing images using VLM
        /// </summary>
        /// <param name="api"></param>
        /// <param name="model">Model to use, must be a VLM</param>
        public TornadoAnalyser(TornadoApi api, Model model)
        {
            _api = api;
            _model = model;
        }

        /// <inheritdoc />
        public async Task<ImageAnalysisResult?> GetImageDescription(Stream imageStream)
        {
            // Convert image to base64
            var image = await Image.LoadAsync(imageStream);
            var mem = new MemoryStream();
            await image.SaveAsPngAsync(mem);
            var buffer = mem.ToArray();
            var base64 = Convert.ToBase64String(buffer);

            // Create conversation
            var conversation = _api.Chat.CreateConversation(new ChatRequest
            {
                Model = _model.ChatModel,
                MaxTokens = 1024,
                Modalities = [ ChatModelModalities.Text, ChatModelModalities.Image ],
            });

            // Setup input
            conversation
               .AppendSystemMessage("You are an image description endpoint. You describe images concisely and accurately, never return a blank response or refuse " +
                                    "to describe an image. Keep responses to 3–10 short sentences. Focus mainly on what is visible, but you may include interpretation (e.g. " +
                                    "explaining visual humour) when it's strongly suggested.");

            conversation.AppendUserInput([ new ChatMessagePart(new ChatImage($"data:image/png;base64,{base64}", "image/png")) ]);
            var description = await conversation.GetResponse();

            if (string.IsNullOrWhiteSpace(description))
                return null;

            conversation.AppendUserInput("Suggest a title for this image (1-5 words)");
            var title = await conversation.GetResponse();

            return new ImageAnalysisResult(title, description);
        }

        /// <summary>
        /// Model to use for image analysis tasks
        /// </summary>
        /// <param name="ChatModel"></param>
        /// <param name="IsLocal"></param>
        public record Model(ChatModel ChatModel, bool IsLocal);
    }
}
