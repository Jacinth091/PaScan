using System;
using System.Threading.Tasks;
using System.Linq;
using PaScan.Models;
using PaScan.Enums;
using PaScan.Models.ViewModels;
using PaScan.Services.Interfaces;
using PaScan.Repositories.Interfaces;
using PaScan.Data;

namespace PaScan.Services;

public class ScanService : IScanService
{
    private readonly AppDbContext _context;
    private readonly IScannerRepository _scannerRepo;
    private readonly IQrTokenRepository _qrTokenRepo;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IScanLogRepository _scanLogRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IRfidCardRepository _rfidCardRepo;

    public ScanService(
        AppDbContext context,
        IScannerRepository scannerRepo,
        IQrTokenRepository qrTokenRepo,
        IDeviceRepository deviceRepo,
        IScanLogRepository scanLogRepo,
        IStudentRepository studentRepo,
        IRfidCardRepository rfidCardRepo)
    {
        _context = context;
        _scannerRepo = scannerRepo;
        _qrTokenRepo = qrTokenRepo;
        _deviceRepo = deviceRepo;
        _scanLogRepo = scanLogRepo;
        _studentRepo = studentRepo;
        _rfidCardRepo = rfidCardRepo;
    }

    public async Task<QrScanResultViewModel> ValidateQrScanAsync(string tokenValue, Guid scannerId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var scanner = await _scannerRepo.GetByIdAsync(scannerId);
            if (scanner == null || scanner.Status != ScannerStatus.ACTIVE
                || (scanner.ScannerType != ScannerType.QR && scanner.ScannerType != ScannerType.BOTH))
            {
                await transaction.RollbackAsync();
                return new QrScanResultViewModel { Allowed = false, DenialReason = "SCANNER_INACTIVE" };
            }

            var token  = await _qrTokenRepo.GetByTokenValueAsync(tokenValue);
            var device = token != null ? await _deviceRepo.GetByIdAsync(token.DeviceId) : null;
            Student? student = null;

            if (device != null)
            {
                student = await _studentRepo.GetByIdAsync(device.StudentId);
            }

            string? denialReason = null;
            if (token == null || device == null) denialReason = "DEVICE_NOT_FOUND";
            else if (token.Status == TokenStatus.REVOKED) denialReason = "REVOKED";
            else if (token.Status == TokenStatus.EXPIRED || token.ExpiresAt < DateTime.UtcNow) denialReason = "EXPIRED";
            else if (device.Status != DeviceStatus.ACTIVE) denialReason = "NOT_APPROVED";

            if (device != null)
            {
                await _scanLogRepo.AddAsync(new GateScanLog
                {
                    DeviceId     = device.Id,
                    QrTokenId    = token?.Id,
                    RfidCardId   = null,
                    ScanType     = ScanType.QR,
                    ScannerId    = scannerId,
                    ScannedAt    = DateTime.UtcNow,
                    IsAllowed    = denialReason == null,
                    DenialReason = denialReason,
                    CreatedAt    = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new QrScanResultViewModel 
            { 
                Allowed = denialReason == null, 
                DenialReason = denialReason,
                DeviceName = device?.DeviceName,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
                StudentNumber = student?.StudentNumber
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<RfidScanResultViewModel> ValidateRfidScanAsync(string cardUid, string apiKey)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var scanner = await _scannerRepo.GetByApiKeyAsync(apiKey);
            if (scanner == null || scanner.Status != ScannerStatus.ACTIVE
                || (scanner.ScannerType != ScannerType.RFID && scanner.ScannerType != ScannerType.BOTH))
            {
                await transaction.RollbackAsync();
                return new RfidScanResultViewModel { Allowed = false, DenialReason = "SCANNER_INACTIVE" };
            }

            var card = await _rfidCardRepo.GetActiveByCardUidAsync(cardUid);
            Student? student = null;

            string? denialReason = null;
            if (card == null) denialReason = "CARD_NOT_FOUND";
            else if (card.Status == TokenStatus.REVOKED) denialReason = "REVOKED";
            else if (card.Status == TokenStatus.EXPIRED || card.SemesterExpiresAt < DateTime.UtcNow) denialReason = "EXPIRED";
            else
            {
                student = await _studentRepo.GetByIdAsync(card.StudentId);
                if (student == null || !student.IsActive) denialReason = "STUDENT_INACTIVE";
                else if (!student.IsRfidEnabled) denialReason = "RFID_DISABLED";
            }

            int deviceCount = 0;

            if (denialReason == null && student != null)
            {
                var activeDevices = await _deviceRepo.GetByStudentIdAsync(student.Id);
                activeDevices = activeDevices.Where(d => d.Status == DeviceStatus.ACTIVE).ToList();
                deviceCount = activeDevices.Count;

                foreach (var device in activeDevices)
                {
                    await _scanLogRepo.AddAsync(new GateScanLog
                    {
                        DeviceId     = device.Id,
                        QrTokenId    = null,
                        RfidCardId   = card.Id,
                        ScanType     = ScanType.RFID,
                        ScannerId    = scanner.Id,
                        ScannedAt    = DateTime.UtcNow,
                        IsAllowed    = true,
                        DenialReason = null,
                        CreatedAt    = DateTime.UtcNow
                    });
                }
            }
            else if (card != null && student != null)
            {
                // Denied. But we still need to log it. Since DeviceId is required, we can't log if there are no devices.
                // We'll log a denial for every device they have. If 0 devices, 0 logs.
                var allDevices = await _deviceRepo.GetByStudentIdAsync(student.Id);
                foreach (var device in allDevices)
                {
                    await _scanLogRepo.AddAsync(new GateScanLog
                    {
                        DeviceId     = device.Id,
                        QrTokenId    = null,
                        RfidCardId   = card.Id,
                        ScanType     = ScanType.RFID,
                        ScannerId    = scanner.Id,
                        ScannedAt    = DateTime.UtcNow,
                        IsAllowed    = false,
                        DenialReason = denialReason,
                        CreatedAt    = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new RfidScanResultViewModel 
            { 
                Allowed = denialReason == null, 
                DenialReason = denialReason,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
                StudentNumber = student?.StudentNumber,
                DeviceCount = deviceCount
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Scanner?> GetScannerByApiKeyAsync(string apiKey)
    {
        return await _scannerRepo.GetByApiKeyAsync(apiKey);
    }
}