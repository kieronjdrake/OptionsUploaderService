using System.Threading;

namespace Prax.Uploader {
    public interface IUploadQueueFactory {
        IUploadQueue Create();
    }
}