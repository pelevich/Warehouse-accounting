using SkiaSharp;

namespace Services.Interfaces.service
{
    public interface IBarcodeService
    {
        public Task<string> GenerateBarcodeNumber();
        public Task<SKImage> GenerateBarcodeCodeImg(string code);
        public Task<bool> ValideCode(string code);
    }
}
