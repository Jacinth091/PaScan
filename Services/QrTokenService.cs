using System;
using PaScan.Services.Interfaces;
using QRCoder;

namespace PaScan.Services;

public class QrTokenService : IQrTokenService
{
    public byte[] GenerateQrCodeImage(string tokenValue)
    {
        if (string.IsNullOrEmpty(tokenValue))
        {
            throw new ArgumentException("Token value cannot be empty", nameof(tokenValue));
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(tokenValue, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}