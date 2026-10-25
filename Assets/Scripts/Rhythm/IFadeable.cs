namespace Brewing.Rhythm
{
    /// <summary>A visual with an overall opacity that <see cref="HelperFader"/> can fade out.</summary>
    public interface IFadeable
    {
        /// <summary>Overall opacity from 0 (invisible) to 1 (fully shown).</summary>
        float Alpha { get; set; }
    }
}
