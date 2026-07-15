namespace CalendarTimeline.Snapbar;

public readonly record struct TimelineHorizontalBounds(double Left, double Width)
{
    public double Right => Left + Width;
}

public static class TimelineCountdownLayout
{
    public static double GetLeft(
        double baseLeft,
        double indicatorWidth,
        double targetLeft,
        IEnumerable<TimelineHorizontalBounds> runningBlocks)
    {
        var candidateLeft = baseLeft;
        var blocks = runningBlocks.ToArray();

        while (true)
        {
            var blockingRight = candidateLeft;
            foreach (var block in blocks)
            {
                if (candidateLeft < block.Right
                    && candidateLeft + indicatorWidth > block.Left)
                {
                    blockingRight = Math.Max(blockingRight, block.Right);
                }
            }

            if (blockingRight <= candidateLeft)
            {
                break;
            }

            candidateLeft = blockingRight;
        }

        return Math.Min(candidateLeft, targetLeft - indicatorWidth);
    }
}
