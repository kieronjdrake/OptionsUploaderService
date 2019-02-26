using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prax.Uploader
{
    public interface IInputSource {
        Task<(string sourceDescription, List<InputPriceData> data)> GetInputData();
        void InputDataUploaded(bool uploadSucceeded);
        string SourceName { get; }
    }
}