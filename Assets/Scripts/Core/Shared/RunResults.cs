namespace Brewing.Minigames
{
    /// <summary>What the results pop-up shows at the end of a run: pass or fail, and up to two lines of detail.</summary>
    public readonly struct RunResults
    {
        public readonly bool Passed;
        public readonly string Detail;
        public readonly string SubDetail;

        public RunResults(bool passed, string detail, string subDetail)
        {
            Passed = passed;
            Detail = detail;
            SubDetail = subDetail;
        }
    }
}
