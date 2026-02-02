using System.Runtime.CompilerServices;

namespace MHServerEmu.Core.Collections
{
    /// <summary>
    /// Generic inline array of 2 elements of type <typeparamref name="T"/>.
    /// </summary>
    [InlineArray(2)]
    public struct InlineArray2<T>
    {
        private T _element0;
    }

    /// <summary>
    /// Generic inline array of 3 elements of type <typeparamref name="T"/>.
    /// </summary>
    [InlineArray(3)]
    public struct InlineArray3<T>
    {
        private T _element0;
    }

    /// <summary>
    /// Generic inline array of 4 elements of type <typeparamref name="T"/>.
    /// </summary>
    [InlineArray(4)]
    public struct InlineArray4<T>
    {
        private T _element0;
    }

    /// <summary>
    /// Generic inline array of 8 elements of type <typeparamref name="T"/>.
    /// </summary>
    [InlineArray(8)]
    public struct InlineArray8<T>
    {
        private T _element0;
    }
}
