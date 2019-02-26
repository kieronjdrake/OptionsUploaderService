using System.Threading;

namespace Prax.Uploader {
    public class UploadQueueFactory : IUploadQueueFactory {
        private readonly CancellationToken _ct;

        public UploadQueueFactory(CancellationToken ct) {
            _ct = ct;
        }

        public IUploadQueue Create() {
            return new UploadQueue(_ct);
        }
    }
}