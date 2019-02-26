namespace Prax.Utils {
    public struct UploadResult {
        public int Successes { get; }
        public int Ignored { get; }
        public int Failures { get; }

        public UploadResult(int successes, int ignored, int failures) {
            Successes = successes;
            Ignored = ignored;
            Failures = failures;
        }

        public static UploadResult Empty() => new UploadResult(0, 0, 0);
        public static UploadResult AllSucceeded(int s) => new UploadResult(s, 0, 0);
        public static UploadResult SingleSuccess() => new UploadResult(1, 0, 0);
        public static UploadResult SingleIgnored() => new UploadResult(0, 1, 0);
        public static UploadResult SingleFailure() => new UploadResult(0, 0, 1);

        public bool Succeeded => Failures == 0;

        public void Deconstruct(out int successes, out int ignored, out int failures) {
            successes = Successes;
            ignored = Ignored;
            failures = Failures;
        }

        public static UploadResult operator +(UploadResult r1, UploadResult r2) {
            return new UploadResult(r1.Successes + r2.Successes, r1.Ignored + r2.Ignored, r1.Failures + r2.Failures);
        }
    }
}