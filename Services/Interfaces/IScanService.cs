using System;
using System.Threading.Tasks;
using PaScan.Models.ViewModels;

namespace PaScan.Services.Interfaces;

public interface IScanService
{
    Task<QrScanResultViewModel> ValidateQrScanAsync(string tokenValue, Guid scannerId);   // T3
    Task<RfidScanResultViewModel> ValidateRfidScanAsync(string cardUid, string apiKey);  // T4
    Task<PaScan.Models.Scanner?> GetScannerByApiKeyAsync(string apiKey);
}