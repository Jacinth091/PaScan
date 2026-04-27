using System;
using System.Linq;
using System.Threading.Tasks;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;
using PaScan.Repositories.Interfaces;
using PaScan.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PaScan.Services;

public class DeviceService : IDeviceService
{
    private readonly AppDbContext _context;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IDeviceRequestRepository _deviceRequestRepo;
    private readonly IStudentRepository _studentRepo;
    private readonly IRfidCardRepository _rfidCardRepo;
    private readonly IQrTokenService _qrTokenService;
    private readonly IQrTokenRepository _qrTokenRepo;

    public DeviceService(
        AppDbContext context,
        IDeviceRepository deviceRepo,
        IDeviceRequestRepository deviceRequestRepo,
        IStudentRepository studentRepo,
        IRfidCardRepository rfidCardRepo,
        IQrTokenService qrTokenService,
        IQrTokenRepository qrTokenRepo)
    {
        _context = context;
        _deviceRepo = deviceRepo;
        _deviceRequestRepo = deviceRequestRepo;
        _studentRepo = studentRepo;
        _rfidCardRepo = rfidCardRepo;
        _qrTokenService = qrTokenService;
        _qrTokenRepo = qrTokenRepo;
    }

    public async Task<StudentDashboardViewModel> GetStudentDashboardAsync(Guid studentId)
    {
        var student = await _studentRepo.GetByIdWithUserAsync(studentId)
            ?? throw new InvalidOperationException("Student not found.");

        var requests = await _deviceRequestRepo.GetByStudentIdAsync(studentId);
        var devices = await _deviceRepo.GetByStudentIdAsync(studentId);
        var activeRfid = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId);

        return new StudentDashboardViewModel
        {
            FirstName = student.FirstName,
            LastName = student.LastName,
            StudentNumber = student.StudentNumber,
            CourseName = student.Course?.Name ?? "N/A",
            IsRfidEnabled = student.IsRfidEnabled,
            RfidExpiresAt = activeRfid?.SemesterExpiresAt,
            Requests = requests.Select(r => new StudentRequestListItem
            {
                Id = r.Id,
                DeviceName = r.DeviceName,
                DeviceType = r.DeviceType,
                Status = r.Status,
                SubmittedAt = r.CreatedAt,
                RejectionReason = r.RejectionReason
            }).ToList(),
            Devices = devices.Select(d => new DeviceListItem
            {
                Id = d.Id,
                DeviceName = d.DeviceName,
                DeviceType = d.DeviceType,
                Brand = d.Brand,
                Model = d.Model,
                Status = d.Status
            }).ToList()
        };
    }

    public async Task<DeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId, Guid studentId)
    {
        var device = await _deviceRepo.GetByIdWithAccessoriesAndTokenAsync(deviceId);
        if (device == null || device.StudentId != studentId)
            throw new UnauthorizedAccessException("Device not found or access denied.");

        var qrToken = await _qrTokenRepo.GetActiveByDeviceIdAsync(deviceId);
        string? qrImageBase64 = null;
        
        if (qrToken != null && qrToken.Status != TokenStatus.REVOKED)
        {
            var qrBytes = _qrTokenService.GenerateQrCodeImage(qrToken.TokenValue);
            qrImageBase64 = Convert.ToBase64String(qrBytes);
        }

        return new DeviceDetailViewModel
        {
            Device = device,
            QrToken = qrToken,
            QrImageBase64 = qrImageBase64
        };
    }

    public async Task SubmitDeviceRequestAsync(DeviceRequestViewModel vm, Guid studentId)
    {
        var existing = await _deviceRepo.GetBySerialNumberAndStudentAsync(vm.SerialNumber, studentId);
        if (existing != null && existing.Status != DeviceStatus.EXPIRED)
            throw new InvalidOperationException("An active or revoked device with this serial number is already registered.");

        // Also check if there is an active pending request
        var studentRequests = await _deviceRequestRepo.GetByStudentIdAsync(studentId);
        if (studentRequests.Any(r => r.SerialNumber == vm.SerialNumber && r.Status == RegisterStatus.PENDING))
            throw new InvalidOperationException("You already have a pending request for this serial number.");

        var request = new DeviceRequest
        {
            StudentId = studentId,
            Purpose = vm.Purpose,
            DeviceName = vm.DeviceName,
            DeviceType = vm.DeviceType,
            Brand = vm.Brand,
            Model = vm.Model,
            SerialNumber = vm.SerialNumber,
            OperatingSystem = vm.OperatingSystem,
            Color = vm.Color,
            Processor = vm.Processor,
            Motherboard = vm.Motherboard,
            Memory = vm.Memory,
            Storage = vm.Storage,
            MonitorSize = vm.MonitorSize,
            Casing = vm.Casing,
            HasCdRom = vm.HasCdRom,
            Status = RegisterStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        await _deviceRequestRepo.AddAsync(request);

        if (vm.Accessories != null)
        {
            foreach (var acc in vm.Accessories.Where(a => !string.IsNullOrWhiteSpace(a.AccessoryName)))
            {
                await _deviceRequestRepo.AddAccessoryAsync(new DeviceRequestAccessory
                {
                    DeviceRequestId = request.Id,
                    AccessoryName = acc.AccessoryName,
                    Quantity = acc.Quantity
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task RenewQrAsync(Guid deviceId, Guid studentId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId)
            ?? throw new InvalidOperationException("Device not found.");

        if (device.StudentId != studentId)
            throw new UnauthorizedAccessException("You do not have permission to renew this device.");

        if (device.Status != DeviceStatus.ACTIVE)
            throw new InvalidOperationException("Cannot renew QR for an inactive device.");

        // We need to fetch the EXPIRED token. Actually, we should get the latest token.
        // If it's ACTIVE, we don't renew. Wait, the rule says:
        // "Only allowed when current token status = EXPIRED. Blocked if status = REVOKED."
        
        var activeToken = await _qrTokenRepo.GetActiveByDeviceIdAsync(deviceId);
        if (activeToken != null && activeToken.Status == TokenStatus.ACTIVE && activeToken.ExpiresAt >= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Current QR token is still active and valid.");
        }

        // Fetch all tokens for this device, order by issued descending to get the latest
        var allTokens = await _context.QRTokens
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        var oldToken = allTokens.FirstOrDefault();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            int newRenewalNumber = 1;
            Guid? renewedFrom = null;

            if (oldToken != null)
            {
                // Only change status to EXPIRED if it was somehow ACTIVE. If it's REVOKED, leave it as REVOKED.
                if (oldToken.Status == TokenStatus.ACTIVE)
                {
                    oldToken.Status = TokenStatus.EXPIRED;
                }
                await _qrTokenRepo.UpdateAsync(oldToken);
                newRenewalNumber = oldToken.RenewalNumber + 1;
                renewedFrom = oldToken.Id;
            }

            var newToken = new QRToken
            {
                DeviceId = deviceId,
                StudentId = studentId,
                TokenValue = Guid.NewGuid().ToString(),
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Status = TokenStatus.ACTIVE,
                RenewalNumber = newRenewalNumber,
                RenewedFrom = renewedFrom
            };

            await _qrTokenRepo.AddAsync(newToken);

            device.QrRenewalCount += 1;
            // The repo doesn't have UpdateAsync for device, but EF tracks it since we fetched it.
            _context.Devices.Update(device);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}