using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Prax.Uploader
{
    public class UploadQueue : IUploadQueue
    {
        private readonly CancellationToken _ct;
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();

        public UploadQueue(CancellationToken ct) {
            _ct = ct;

            Task.Run(() => {
                while (!_queue.IsCompleted) {
                    Action action = null;
                    try {
                        action = _queue.Take();
                    }
                    catch (InvalidOperationException) { } // Thrown if IsCompleted set by another thread druing Take
                    action?.Invoke();
                }
            }, ct);
        }

        public void SetCompleted() {
            _queue.CompleteAdding();
        }

        public bool AddAction(Action a) {
            // TODO Timeout here, not Infinite(-1) ? Even though we have a cancellation token?
            return _queue.TryAdd(a, -1, _ct);
        }
    }
}
