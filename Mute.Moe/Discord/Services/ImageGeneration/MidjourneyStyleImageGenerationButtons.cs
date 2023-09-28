namespace Mute.Moe.Discord.Services.ImageGeneration;

public class MidjourneyStyleImageGenerationButtons
{
    public const string IdPrefix = "MJButton";

    private const string VariantButtonFullId = IdPrefix + VariantButtonId + "_";
    private const string OutpaintButtonFullId = IdPrefix + OutpaintButtonId + "_";
    private const string UpscaleButtonFullId = IdPrefix + UpscaleButtonId + "_";
    public const string RedoButtonFullId = IdPrefix + RedoButtonId + "_";

    public const string VariantButtonId = "VariantButtonId";
    public const string OutpaintButtonId = "OutpaintButtonId";
    public const string UpscaleButtonId = "UpscaleButtonId";
    public const string RedoButtonId = "RedoButtonId";

    public static string GetOutpaintButtonId(int index)
    {
        return OutpaintButtonFullId + index;
    }

    public static string GetVariantButtonId(int index)
    {
        return VariantButtonFullId + index;
    }

    public static string GetUpscaleButtonId(int index)
    {
        return UpscaleButtonFullId + index;
    }

    public static string GetRedoButtonId()
    {
        return RedoButtonFullId;
    }
}