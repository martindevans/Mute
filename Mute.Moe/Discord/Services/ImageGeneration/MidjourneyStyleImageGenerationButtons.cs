namespace Mute.Moe.Discord.Services.ImageGeneration;

/// <summary>
/// ID strings for *MuteJourney buttons.
///
/// These buttons have an ID like: [Common][Button]_[Index]
/// - Handlers for all *MuteJourney buttons can be setup by binding on [Common]*
/// - Handlers which inject the index can be setup by binding on [Common]*_*
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


    /// <summary>
    /// Middle bit of the variant button ID
    /// </summary>
    public const string VariantButtonPrefix = "VariantButtonId";

    /// <summary>
    /// Middle bit of the outpaint button ID
    /// </summary>
    public const string OutpaintButtonPrefix = "OutpaintButtonId";

    /// <summary>
    /// Middle bit of the upscale button ID
    /// </summary>
    public const string UpscaleButtonPrefix = "UpscaleButtonId";

    /// <summary>
    /// Middle bit of the redo button ID
    /// </summary>
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