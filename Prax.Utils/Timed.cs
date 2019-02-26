using System;
using System.Diagnostics;

namespace Prax.Utils
{
    public static class Timed {
        public static TimeSpan RunAction(Action a) {
            var sw = new Stopwatch();
            sw.Start();
            a();
            sw.Stop();
            return sw.Elapsed;
        }

        public static (T result, TimeSpan elapsed) RunFunction<T>(Func<T> f) {
            var sw = new Stopwatch();
            sw.Start();
            var res = f();
            sw.Stop();
            return (res, sw.Elapsed);
        }
    }
}
