using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.Tests.Collections
{
    public class PickerTests
    {
        [Fact]
        public void Dispose_ReturnsPickerToPool()
        {
            var picker = Picker<int>.Create(new GRandom(1));
            picker.Add(42, 1);
            picker.Dispose();
            Assert.True(picker.IsInPool);
        }

        [Fact]
        public void Create_AfterDispose_ReusesSameInstance()
        {
            var first = Picker<int>.Create(new GRandom(1));
            first.Dispose();
            var second = Picker<int>.Create(new GRandom(2));
            Assert.Same(first, second); // pool returned the same object
        }

        [Fact]
        public void ResetForPool_ClearsAllState()
        {
            var picker = new Picker<int>(new GRandom(1));
            picker.Add(1, 10);
            picker.Add(2, 20);
            picker.ResetForPool();
            Assert.True(picker.Empty());
        }
    }
}
