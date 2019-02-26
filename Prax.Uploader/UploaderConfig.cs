namespace Prax.Uploader {
    public interface IUploaderConfig {
        bool DryRun { get; }
        string DefaultPricingGroup { get; }
        int UploadChunkSize { get; }
        int? MaxOptionsToUpload { get; }
        int? OptionsToSkip { get; }
        bool MarkProcessedOnceUploaded { get; }
        int? WaitIntervalBetweenBatchesMs { get; }
        bool ForceUploadOldTradeDates { get; }
        TradeDateMapping TradeDateMapping { get; }
    }
}