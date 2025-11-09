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
        public Handle CreateTask()
        {
            lock (_pendingTasks)
            {
                ulong taskId = _currentTaskId++;
                TaskCompletionSource<T> tcs = new();
                _pendingTasks.Add(taskId, tcs);
                return new(taskId, tcs.Task, this);
            }
        }

        /// <summary>
        /// Completes the <see cref="Task{TResult}"/> with the specified id using the provided result data. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool CompleteTask(ulong taskId, T result)
        {
            TaskCompletionSource<T> tcs = null;

            lock (_pendingTasks)
            {
                if (_pendingTasks.Remove(taskId, out tcs) == false)
                    return false;
            }

            tcs.SetResult(result);
            return true;
        }

        /// <summary>
        /// Cancels the <see cref="Task{TResult}"/> with the specified id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool CancelTask(ulong taskId)
        {
            TaskCompletionSource<T> tcs = null;

            lock (_pendingTasks)
            {
                if (_pendingTasks.Remove(taskId, out tcs) == false)
                    return false;
            }

            tcs.SetCanceled();
            return true;
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

        /// <summary>
        /// Represents a <see cref="Task{TResult}"/> managed by a <see cref="TaskManager{T}"/>.
        /// </summary>
        public readonly struct Handle
        {
            public readonly ulong Id;
            public readonly Task<T> Task;
            public readonly TaskManager<T> Manager;

            internal Handle(ulong id, Task<T> task, TaskManager<T> manager)
            {
                Id = id;
                Task = task;
                Manager = manager;
            }

            /// <summary>
            /// Cancels the <see cref="Task{TResult}"/> represented by this handle.
            /// </summary>
            public void Cancel()
            {
                Manager.CancelTask(Id);
            }
        }
    }
}
