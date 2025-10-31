namespace MHServerEmu.Core.Threading
{
    /// <summary>
    /// Manages awaitable custom <see cref="Task{TResult}"/> instances that can be manually completed.
    /// </summary>
    public class TaskManager<T>
    {
        private readonly Dictionary<ulong, TaskCompletionSource<T>> _pendingTasks = new();
        private ulong _currentTaskId = 1;

        /// <summary>
        /// Constructs a new <see cref="Task{TResult}"/>.
        /// </summary>
        public Task<T> CreateTask(out ulong taskId)
        {
            lock (_pendingTasks)
            {
                taskId = _currentTaskId++;
                TaskCompletionSource<T> tcs = new();
                _pendingTasks.Add(taskId, tcs);
                return tcs.Task;
            }
        }

        /// <summary>
        /// Completes the <see cref="Task{TResult}"/> with the specified id using the provided result data. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool CompleteTask(ulong taskId, T result)
        {
            lock (_pendingTasks)
            {
                if (_pendingTasks.Remove(taskId, out TaskCompletionSource<T> tcs) == false)
                    return false;

                tcs.SetResult(result);
                return true;
            }
        }

        /// <summary>
        /// Cancels the <see cref="Task{TResult}"/> with the specified id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool CancelTask(ulong taskId)
        {
            lock (_pendingTasks)
            {
                if (_pendingTasks.Remove(taskId, out TaskCompletionSource<T> tcs) == false)
                    return false;

                tcs.SetCanceled();
                return true;
            }
        }

        /// <summary>
        /// Cancels all current tasks.
        /// </summary>
        public void CancelAllTasks()
        {
            lock (_pendingTasks)
            {
                foreach (var kvp in _pendingTasks)
                    kvp.Value.SetCanceled();

                _pendingTasks.Clear();
            }
        }
    }
}
