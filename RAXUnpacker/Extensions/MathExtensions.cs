namespace RAXUnpacker.Extensions
{
    internal static class MathExtensions
    {
        internal static int Align(this int value, int alignment)
            => value + value.Remaining(alignment);

        internal static int Remaining(this int value, int alignment)
        {
            int remaining = value % alignment;
            if (remaining == 0)
            {
                return remaining;
            }

            return alignment - remaining;
        }

        internal static long Remaining(this long value, long alignment)
        {
            long remaining = value % alignment;
            if (remaining == 0)
            {
                return remaining;
            }

            return alignment - remaining;
        }
    }
}
