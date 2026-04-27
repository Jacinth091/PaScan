using System;

namespace PaScan.Services.Interfaces;

public interface IQrTokenService
{
    byte[] GenerateQrCodeImage(string tokenValue);
}