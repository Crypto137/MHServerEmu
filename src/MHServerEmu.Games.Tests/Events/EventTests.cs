using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Tests.Events
{
    public class EventTests
    {
        [Fact]
        public void Invoke_RemoveWhileIterating_IterationContinues()
        {
            const int HandlerCount = 15;
            const int RemoveIndex = 7;

            Event<FakeEvent> @event = new();
            List<Event<FakeEvent>.Action> handlers = new();
            bool[] results = new bool[HandlerCount];

            for (int i = 0; i < HandlerCount; i++)
            {
                int index = i;

                if (i == RemoveIndex)
                {
                    handlers.Add((in FakeEvent data) =>
                    {
                        results[index] = true;
                        @event.RemoveAction(handlers[RemoveIndex + 1]);
                    });
                }
                else
                {
                    handlers.Add((in FakeEvent data) =>
                    {
                        results[index] = true;
                    });
                }

                @event.AddActionBack(handlers[i]);
            }

            // Check if everything but the removed event executed
            @event.Invoke(new(0));
            for (int i = 0; i < HandlerCount; i++)
            {
                if (i == RemoveIndex + 1)
                    Assert.False(results[i]);
                else
                    Assert.True(results[i]);
            }

            // Check again after removal
            Array.Clear(results);
            @event.Invoke(new(0));
            for (int i = 0; i < HandlerCount; i++)
            {
                if (i == RemoveIndex + 1)
                    Assert.False(results[i]);
                else
                    Assert.True(results[i]);
            }
        }

        private readonly struct FakeEvent(int value) : IGameEventData
        {
            public readonly int Value = value;
        }
    }
}
