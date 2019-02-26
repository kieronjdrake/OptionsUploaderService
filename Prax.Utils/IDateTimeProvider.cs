using System;

namespace Prax.Utils
{
    public interface IDateTimeProvider {
        DateTime Now { get; }
        DateTime UtcNow { get; }
        DateTime Today { get; }
    }
}
