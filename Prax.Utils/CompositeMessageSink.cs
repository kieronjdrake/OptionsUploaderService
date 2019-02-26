using System;
using System.Collections.Generic;
using System.Linq;

namespace Prax.Utils {
    public sealed class CompositeMessageSink : IMessageSink {
        private readonly List<IMessageSink> _sinks;

        public CompositeMessageSink(IEnumerable<IMessageSink> sinks) {
            _sinks = sinks?.ToList() ?? throw new ArgumentNullException(nameof(sinks));
        }

        public void Close() {
            _sinks.ForEach(s => s.Close());
        }

        public void SendMessage(string message, bool isPrimaryAspectInstance) {
            _sinks.ForEach(s => s.SendMessage(message, isPrimaryAspectInstance));
        }
    }
}
