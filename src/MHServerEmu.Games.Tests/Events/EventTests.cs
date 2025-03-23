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

            Event<int> @event = new();
            List<Action<int>> handlers = new();
            bool[] results = new bool[HandlerCount];

            for (int i = 0; i < HandlerCount; i++)
            {
                int index = i;

                if (i == RemoveIndex)
                {
                    handlers.Add(eventType =>
                    {
                        results[index] = true;
                        @event.RemoveAction(handlers[RemoveIndex + 1]);
                    });
                }
                else
                {
                    handlers.Add(eventType =>
                    {
                        results[index] = true;
                    });
                }

                @event.AddActionBack(handlers[i]);
            }

            // Check if everything but the removed event executed
            @event.Invoke(0);
            for (int i = 0; i < HandlerCount; i++)
            {
                if (i == RemoveIndex + 1)
                    Assert.False(results[i]);
                else
                    Assert.True(results[i]);
            }

            // Check again after removal
            Array.Clear(results);
            @event.Invoke(0);
            for (int i = 0; i < HandlerCount; i++)
            {
                if (i == RemoveIndex + 1)
                    Assert.False(results[i]);
                else
                    Assert.True(results[i]);
            }
        }
    }
}
