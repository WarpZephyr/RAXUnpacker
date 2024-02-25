namespace RAXUnpacker.Extensions
{
    internal static class MathExtensions
    {
        internal static int Align(this int value, int alignment)
        {
            return value + (alignment - value % alignment);
        }
    }
}
