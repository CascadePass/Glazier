#region Using Directives

#endregion

namespace CascadePass.Glazier.Core
{
    public enum OnyxProcessingMode
    {
        None,

        /// <summary>
        /// Preserves facial details, avoids over-sharpening.
        /// </summary>
        Portrait,

        /// <summary>
        /// Enhances sharpness while keeping natural gradients
        /// </summary>
        Landscape,

        /// <summary>
        /// Adjusts edge sensitivity based on a dark image
        /// </summary>
        LowLight,

        /// <summary>
        /// Adjusts edge sensitivity based on a bright image
        /// </summary>
        HighKey,
    }
}