using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Random;

namespace MHServerEmu.Core.Tests.Collections
{
    public class PickerTests
    {
        private static GRandom MakeRandom(int seed = 42) => new(seed);

        [Fact]
        public void Pick_UnweightedSingleElement_ReturnsThatElement()
        {
            var picker = new Picker<int>(MakeRandom());
            picker.Add(99);
            Assert.Equal(99, picker.Pick());
        }

        [Fact]
        public void Pick_WeightedSingleElement_ReturnsThatElement()
        {
            var picker = new Picker<int>(MakeRandom());
            picker.Add(77, 100);
            Assert.Equal(77, picker.Pick());
        }

        [Fact]
        public void Add_ZeroWeightElement_IsIgnored()
        {
            var picker = new Picker<int>(MakeRandom());
            picker.Add(1, 0);
            Assert.True(picker.Empty());
        }

        [Fact]
        public void GetRandomIndexWeighted_DistributionIsCorrect()
        {
            var picker = new Picker<int>(MakeRandom(0));
            picker.Add(1, 1);
            picker.Add(99, 99);

            int countHigh = 0;
            const int Trials = 10_000;
            for (int i = 0; i < Trials; i++)
                if (picker.Pick() == 99)
                    countHigh++;

            Assert.InRange(countHigh, (int)(Trials * 0.97), Trials);
        }

        [Fact]
        public void GetRandomIndexWeighted_AfterPickRemove_DistributionAdjusts()
        {
            var picker = new Picker<string>(MakeRandom(1));
            picker.Add("a", 50);
            picker.Add("b", 50);

            picker.PickRemove(out string removed);
            string remaining = removed == "a" ? "b" : "a";

            for (int i = 0; i < 100; i++)
                Assert.Equal(remaining, picker.Pick());
        }

        [Fact]
        public void GetRandomIndexWeighted_AfterClear_PickerIsEmpty()
        {
            var picker = new Picker<int>(MakeRandom());
            picker.Add(1, 10);
            picker.Add(2, 20);
            picker.Clear();
            Assert.True(picker.Empty());
        }

        [Fact]
        public void GetRandomIndexWeighted_AfterRemoveIndex_DistributionExcludesRemoved()
        {
            var picker = new Picker<int>(MakeRandom(2));
            picker.Add(1, 1);
            picker.Add(2, 99);
            picker.RemoveIndex(1);

            for (int i = 0; i < 100; i++)
                Assert.Equal(1, picker.Pick());
        }

        [Fact]
        public void GetRandomIndexWeighted_AfterClearAndReAdd_PicksCorrectly()
        {
            var picker = new Picker<int>(MakeRandom(3));
            picker.Add(1, 50);
            picker.Add(2, 50);
            picker.Clear();
            Assert.True(picker.Empty());

            picker.Add(7, 100);
            for (int i = 0; i < 50; i++)
                Assert.Equal(7, picker.Pick());
        }

        [Fact]
        public void GetRandomIndexWeighted_AfterInitialize_PicksCorrectly()
        {
            var picker = new Picker<int>();
            picker.Initialize(MakeRandom(4));
            picker.Add(42, 100);
            for (int i = 0; i < 50; i++)
                Assert.Equal(42, picker.Pick());
        }

        [Fact]
        public void GetRandomIndexWeighted_CopyConstructor_PicksCorrectly()
        {
            var original = new Picker<int>(MakeRandom(5));
            original.Add(1, 1);
            original.Add(99, 99);

            var copy = new Picker<int>(original);
            int countHigh = 0;
            const int Trials = 1000;
            for (int i = 0; i < Trials; i++)
                if (copy.Pick() == 99)
                    countHigh++;

            Assert.InRange(countHigh, (int)(Trials * 0.90), Trials);
        }
    }
}
