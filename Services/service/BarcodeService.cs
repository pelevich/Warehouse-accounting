using BarcodeStandard;
using Services.Interfaces.repository;
using Services.Interfaces.service;
using SkiaSharp;
using System.Diagnostics;

namespace Services.service
{
    public class BarcodeService : IBarcodeService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BarcodeService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateBarcodeNumber()
        {
            var allProduct = await _unitOfWork.ProductRepository.GetAllAsync();
            var allBarcodes = allProduct
                                .Where(p => !string.IsNullOrEmpty(p.Barcode))
                                .Select(p => p.Barcode)
                                .ToList();

            string code = "";

            do
            {
                code = "";
                int sum = 0;
                var random = new Random();
                for (int i = 0; i < 12; i++)
                {
                    code += random.Next(0, 10).ToString();
                    int digit = code[i] - '0';
                    if (i % 2 == 0)
                    {
                        sum += digit;
                    }
                    else
                    {
                        sum += digit * 3;
                    }
                }
                int checkDigit = (10 - (sum % 10)) % 10;
                code += checkDigit.ToString();
            } while (allBarcodes.Contains(code) == true);

            return code;
        }

        public SKImage GenerateBarcodeCodeImg(string code)
        {
            var barcode = new Barcode
            {
                IncludeLabel = true
            };

            var image = barcode.Encode(
                BarcodeStandard.Type.Ean13,
                code
            );

            return image;
        }

        public async Task<bool> ValideCode(string code)
        {
            int sum = 0;

            for (int i = 0; i < 12; i++)
            {
                int digit = code[i] - '0';
                if (i % 2 == 0)
                {
                    sum += digit;
                }
                else
                {
                    sum += digit * 3;
                }
            }

            int checkDigit = (10 - (sum % 10)) % 10;

            return checkDigit == code[^1] - '0';
        }
    }
}
