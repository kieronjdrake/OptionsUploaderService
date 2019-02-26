using System;

namespace Prax.Uploader
{
    public interface IUploadQueue {
        bool AddAction(Action a);
        void SetCompleted();
    }
}