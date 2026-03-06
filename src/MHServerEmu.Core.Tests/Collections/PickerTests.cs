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
        public void Create_ReturnedInstance_IsClean()
        {
            // Create, use, dispose, then get again — should be fully clean
            var picker = Picker<int>.Create(new GRandom(1));
            picker.Add(1, 10);
            picker.Add(2, 20);
            picker.Dispose();

            // Get a new one (may or may not be the same object — doesn't matter)
            var picker2 = Picker<int>.Create(new GRandom(2));
            Assert.True(picker2.Empty());  // reused instance was reset

            picker2.Dispose();
        }

        [Fact]
        public void ResetForPool_ClearsAllState_AllowsWeightedAndUnweightedReuse()
        {
            // Start weighted, reset, then verify unweighted Add works (not stuck in Weighted mode)
            var picker = Picker<int>.Create(new GRandom(1));
            picker.Add(1, 10);  // puts picker in Weighted mode
            picker.Dispose();   // returns to pool (calls ResetForPool)

            var picker2 = Picker<int>.Create(new GRandom(1));
            picker2.Add(42);    // unweighted — this would fail if _weightMode were stuck at Weighted
            int result = picker2.Pick();
            Assert.Equal(42, result);
            picker2.Dispose();
        }
    }
}
