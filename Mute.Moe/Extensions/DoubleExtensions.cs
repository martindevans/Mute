namespace Mute.Moe.Extensions;

/// <summary>
/// 
/// </summary>
public static class DoubleExtensions
{
    /// <summary>
    /// Converts a logit value to a probability (0 to 1).
    /// </summary>
    public static double LogitToProbability(this double logit)
    {
        return (1.0 / (1.0 + Math.Exp(-logit)));
    }

    /// <summary>
    /// Converts a logit value to a probability (0 to 1).
    /// </summary>
    public static float LogitToProbability(this float logit)
    {
        return (1.0f / (1.0f + MathF.Exp(-logit)));
    }
}