namespace Mute.Moe.Discord.Services.ImageGeneration;

/// <summary>
/// ID strings for *MuteJourney buttons
/// </summary>
public static class MidjourneyStyleImageGenerationButtons
{
    /// <summary>
    /// All these buttons are prefixed with this
    /// </summary>
    public const string CommonPrefix = "MJButton";

    /// <summary>
    /// Full ID of the "variant" button (except for the index on the end)
    /// </summary>
    private const string VariantButtonFullId = CommonPrefix + VariantButtonPrefix + "_";

    /// <summary>
    /// Full ID of the "Outpaint" button (except for the index on the end)
    /// </summary>
    private const string OutpaintButtonFullId = CommonPrefix + OutpaintButtonPrefix + "_";

    /// <summary>
    /// Full ID of the "Upscale" button (except for the index on the end)
    /// </summary>
    private const string UpscaleButtonFullId = CommonPrefix + UpscaleButtonPrefix + "_";

    /// <summary>
    /// Full ID of the "Redo" button
    /// </summary>
    public const string RedoButtonFullId = CommonPrefix + RedoButtonPrefix;

    public const string VariantButtonPrefix = "VariantButtonId";
    public const string OutpaintButtonPrefix = "OutpaintButtonId";
    public const string UpscaleButtonPrefix = "UpscaleButtonId";
    public const string RedoButtonPrefix = "RedoButtonId";

    /// <summary>
    /// Get the ID for the outpaint button
    /// </summary>
    /// <param name="index">Index of which image to outpaint</param>
    /// <returns></returns>
    public static string GetOutpaintButtonId(int index)
    {
        return OutpaintButtonFullId + index;
    }

    /// <summary>
    /// Get the ID for the variant button
    /// </summary>
    /// <param name="index">Index of which image to generate a variant of</param>
    /// <returns></returns>
    public static string GetVariantButtonId(int index)
    {
        return VariantButtonFullId + index;
    }

    /// <summary>
    /// Get the ID for the upscale button
    /// </summary>
    /// <param name="index">Index of which image to upscale</param>
    /// <returns></returns>
    public static string GetUpscaleButtonId(int index)
    {
        return UpscaleButtonFullId + index;
    }

    /// <summary>
    /// Get the ID for the redo button
    /// </summary>
    /// <returns></returns>
    public static string GetRedoButtonId()
    {
        return RedoButtonFullId;
    }
}