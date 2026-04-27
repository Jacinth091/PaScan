using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Models.ViewModels;
using PaScan.Enums;
using PaScan.Services.Interfaces;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;

namespace PaScan.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IDeviceRequestRepository _deviceRequestRepo;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IQrTokenRepository _qrTokenRepo;

    public AdminService(
        AppDbContext context,
        IDeviceRequestRepository deviceRequestRepo,
        IDeviceRepository deviceRepo,
        IQrTokenRepository qrTokenRepo)
    {
        _context = context;
        _deviceRequestRepo = deviceRequestRepo;
        _deviceRepo = deviceRepo;
        _qrTokenRepo = qrTokenRepo;
    }

    public async Task<AdminRequestListViewModel> GetPendingRequestsAsync()
    {
        var requests = await _deviceRequestRepo.GetAllPendingAsync();
        return new AdminRequestListViewModel
        {
            Requests = requests.Select(r => new PendingRequestListItem
            {
                Id = r.Id,
                StudentName = r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "Unknown",
                StudentNumber = r.Student?.StudentNumber ?? "Unknown",
                DeviceType = r.DeviceType,
                Brand = r.Brand,
                Model = r.Model,
                SubmittedAt = r.CreatedAt
            }).ToList()
        };
    }

    public async Task<AdminRequestDetailViewModel> GetRequestDetailAsync(Guid requestId)
    {
        var request = await _deviceRequestRepo.GetByIdWithAccessoriesAsync(requestId)
            ?? throw new InvalidOperationException("Request not found.");

        return new AdminRequestDetailViewModel
        {
            Request = new RequestDetailViewModel
            {
                Id = request.Id,
                StudentName = request.Student != null ? $"{request.Student.FirstName} {request.Student.LastName}" : "Unknown",
                StudentNumber = request.Student?.StudentNumber ?? "Unknown",
                CourseName = request.Student?.Course?.Name ?? "Unknown",
                Purpose = request.Purpose,
                DeviceName = request.DeviceName,
                DeviceType = request.DeviceType,
                Brand = request.Brand,
                Model = request.Model,
                SerialNumber = request.SerialNumber,
                OperatingSystem = request.OperatingSystem,
                Color = request.Color,
                Processor = request.Processor,
                Motherboard = request.Motherboard,
                Memory = request.Memory,
                Storage = request.Storage,
                MonitorSize = request.MonitorSize,
                Casing = request.Casing,
                HasCdRom = request.HasCdRom,
                Status = request.Status,
                SubmittedAt = request.CreatedAt,
                Accessories = request.Accessories?.Select(a => new AccessoryViewModel
                {
                    AccessoryName = a.AccessoryName,
                    Quantity = a.Quantity
                }).ToList() ?? new List<AccessoryViewModel>()
            }
        };
    }

    public async Task ApproveDeviceRequestAsync(Guid requestId, Guid adminId)
    {
        var request = await _deviceRequestRepo.GetByIdWithAccessoriesAsync(requestId)
            ?? throw new InvalidOperationException("Request not found.");

        if (request.Status != RegisterStatus.PENDING)
            throw new InvalidOperationException("This request has already been reviewed.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Step 1 — Update request
            request.Status     = RegisterStatus.APPROVED;
            request.ReviewedBy = adminId;
            request.ReviewedAt = DateTime.UtcNow;

            // Step 2 — Create or update device
            var existingDevice = await _context.Devices
                .Include(d => d.Accessories)
                .FirstOrDefaultAsync(d => d.SerialNumber == request.SerialNumber && d.StudentId == request.StudentId);

            Device device;
            if (existingDevice != null)
            {
                if (existingDevice.Status == DeviceStatus.REVOKED)
                    throw new InvalidOperationException("Cannot approve a request for a revoked device.");
                if (existingDevice.Status == DeviceStatus.ACTIVE)
                    throw new InvalidOperationException("This device is already active.");

                device = existingDevice;
                device.OriginalRequestId  = request.Id;
                device.DeviceName         = request.DeviceName;
                device.DeviceType         = request.DeviceType;
                device.Brand              = request.Brand;
                device.Model              = request.Model;
                device.OperatingSystem    = request.OperatingSystem;
                device.Color              = request.Color;
                device.Processor          = request.Processor;
                device.Motherboard        = request.Motherboard;
                device.Memory             = request.Memory;
                device.Storage            = request.Storage;
                device.MonitorSize        = request.MonitorSize;
                device.Casing             = request.Casing;
                device.HasCdRom           = request.HasCdRom;
                device.Purpose            = request.Purpose ?? "Educational use";
                device.Status             = DeviceStatus.ACTIVE;
                device.ApprovedBy         = adminId;
                device.ApprovedAt         = DateTime.UtcNow;

                _context.Devices.Update(device);

                // Remove old accessories to replace with new ones
                if (device.Accessories.Any())
                {
                    _context.DeviceAccessories.RemoveRange(device.Accessories);
                }
            }
            else
            {
                device = new Device
                {
                    StudentId          = request.StudentId,
                    OriginalRequestId  = request.Id,
                    DeviceName         = request.DeviceName,
                    DeviceType         = request.DeviceType,
                    Brand              = request.Brand,
                    Model              = request.Model,
                    SerialNumber       = request.SerialNumber,
                    OperatingSystem    = request.OperatingSystem,
                    Color              = request.Color,
                    Processor          = request.Processor,
                    Motherboard        = request.Motherboard,
                    Memory             = request.Memory,
                    Storage            = request.Storage,
                    MonitorSize        = request.MonitorSize,
                    Casing             = request.Casing,
                    HasCdRom           = request.HasCdRom,
                    Purpose            = request.Purpose ?? "Educational use",
                    Status             = DeviceStatus.ACTIVE,
                    ApprovedBy         = adminId,
                    ApprovedAt         = DateTime.UtcNow,
                    QrRenewalCount     = 0
                };
                await _deviceRepo.AddAsync(device);
            }

            // Step 3 — Copy accessories
            if (request.Accessories != null)
            {
                foreach (var acc in request.Accessories)
                {
                    await _deviceRepo.AddAccessoryAsync(new DeviceAccessory
                    {
                        DeviceId      = device.Id,
                        AccessoryName = acc.AccessoryName,
                        Quantity      = acc.Quantity
                    });
                }
            }

            // Step 4 — Issue QR token
            await _qrTokenRepo.AddAsync(new QRToken
            {
                DeviceId      = device.Id,
                StudentId     = request.StudentId,
                TokenValue    = Guid.NewGuid().ToString(),
                IssuedAt      = DateTime.UtcNow,
                ExpiresAt     = DateTime.UtcNow.AddDays(30),
                Status        = TokenStatus.ACTIVE,
                RenewalNumber = existingDevice != null ? (existingDevice.QrRenewalCount + 1) : 0,
                RenewedFrom   = null
            });

            if (existingDevice != null)
            {
                device.QrRenewalCount += 1;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RejectDeviceRequestAsync(Guid requestId, Guid adminId, string reason)
    {
        var request = await _deviceRequestRepo.GetByIdWithAccessoriesAsync(requestId)
            ?? throw new InvalidOperationException("Request not found.");

        if (request.Status != RegisterStatus.PENDING)
            throw new InvalidOperationException("This request has already been reviewed.");

        request.Status = RegisterStatus.REJECTED;
        request.ReviewedBy = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = reason;

        await _context.SaveChangesAsync();
    }

    public async Task<AdminStudentListViewModel> GetAllStudentsAsync()
    {
        var students = await _context.Students
            .Include(s => s.Course)
            .OrderBy(s => s.LastName)
            .ToListAsync();
            
        return new AdminStudentListViewModel { Students = students };
    }

    public async Task<AdminDeviceListViewModel> GetAllDevicesAsync()
    {
        var devices = await _context.Devices
            .Include(d => d.Student)
            .OrderByDescending(d => d.ApprovedAt)
            .ToListAsync();
            
        return new AdminDeviceListViewModel { Devices = devices };
    }

    public async Task<AdminRfidListViewModel> GetAllRfidCardsAsync()
    {
        var cards = await _context.RFIDCards
            .Include(r => r.Student)
            .OrderByDescending(r => r.IssuedAt)
            .ToListAsync();
            
        return new AdminRfidListViewModel { RfidCards = cards };
    }

    public async Task<AdminDeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId)
    {
        var device = await _context.Devices
            .Include(d => d.Student)
            .ThenInclude(s => s.Course)
            .Include(d => d.Accessories)
            .FirstOrDefaultAsync(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("Device not found.");

        var qrToken = await _qrTokenRepo.GetActiveByDeviceIdAsync(deviceId);

        return new AdminDeviceDetailViewModel
        {
            Device = device,
            QrToken = qrToken
        };
    }

    public async Task RevokeQrTokenAsync(Guid deviceId, RevocationReason reason)
    {
        var token = await _qrTokenRepo.GetActiveByDeviceIdAsync(deviceId)
            ?? throw new InvalidOperationException("No active QR token found for this device.");

        token.Status = TokenStatus.REVOKED;
        token.RevokedAt = DateTime.UtcNow;
        token.RevocationReason = reason;

        _context.QRTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeDeviceAsync(Guid deviceId)
    {
        var device = await _deviceRepo.GetByIdAsync(deviceId)
            ?? throw new InvalidOperationException("Device not found.");

        device.Status = DeviceStatus.REVOKED;
        _context.Devices.Update(device);

        // Also revoke the QR token if active
        var token = await _qrTokenRepo.GetActiveByDeviceIdAsync(deviceId);
        if (token != null)
        {
            token.Status = TokenStatus.REVOKED;
            token.RevokedAt = DateTime.UtcNow;
            token.RevocationReason = RevocationReason.ADMIN_REVOKED;
            _context.QRTokens.Update(token);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<AdminDashboardViewModel> GetDashboardStatsAsync()
    {
        var pendingCount = await _context.DeviceRequests.CountAsync(r => r.Status == RegisterStatus.PENDING);
        var activeDeviceCount = await _context.Devices.CountAsync(d => d.Status == DeviceStatus.ACTIVE);
        var totalStudentCount = await _context.Students.CountAsync(s => s.IsActive);
        
        var today = DateTime.UtcNow.Date;
        var todayScanCount = await _context.GateScanLogs.CountAsync(l => l.ScannedAt >= today);

        var recentPending = await _context.DeviceRequests
            .Include(r => r.Student)
            .Where(r => r.Status == RegisterStatus.PENDING)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new PendingRequestListItem
            {
                Id = r.Id,
                StudentName = r.Student != null ? $"{r.Student.FirstName} {r.Student.LastName}" : "Unknown",
                DeviceType = r.DeviceType,
                Brand = r.Brand,
                Model = r.Model,
                SubmittedAt = r.CreatedAt
            })
            .ToListAsync();

        var recentLogs = await _context.GateScanLogs
            .Include(l => l.Scanner)
            .Include(l => l.Device)
            .OrderByDescending(l => l.ScannedAt)
            .Take(10)
            .Select(l => new ScanLogListItem
            {
                Id = l.Id,
                DeviceName = l.Device.DeviceName,
                StudentName = _context.Students.Where(s => s.Id == l.Device.StudentId).Select(s => $"{s.FirstName} {s.LastName}").FirstOrDefault(),
                ScanType = l.ScanType,
                ScannerName = l.Scanner.Name,
                IsAllowed = l.IsAllowed,
                DenialReason = l.DenialReason,
                ScannedAt = l.ScannedAt
            })
            .ToListAsync();

        var activeScanners = await _context.Scanners
            .Where(s => s.Status == ScannerStatus.ACTIVE)
            .Select(s => new ScannerListItem
            {
                Id = s.Id,
                Name = s.Name,
                Location = s.Location,
                ScannerType = s.ScannerType,
                Status = s.Status
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            PendingRequestCount = pendingCount,
            ActiveDeviceCount = activeDeviceCount,
            TotalStudentCount = totalStudentCount,
            TodayScanCount = todayScanCount,
            RecentPendingRequests = recentPending,
            RecentScanLogs = recentLogs,
            ActiveScanners = activeScanners
        };
    }

    public async Task<AdminStudentDetailViewModel> GetStudentDetailAsync(Guid studentId)
    {
        var student = await _context.Students
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new InvalidOperationException("Student not found.");

        var devices = await _deviceRepo.GetByStudentIdAsync(studentId);
        var activeRfid = await _context.RFIDCards
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.Status == TokenStatus.ACTIVE);

        return new AdminStudentDetailViewModel
        {
            Student = student,
            Devices = devices,
            ActiveRfid = activeRfid
        };
    }

    public async Task<ScannerListViewModel> GetAllScannersAsync()
    {
        var scanners = await _context.Scanners
            .Select(s => new ScannerListItem
            {
                Id = s.Id,
                Name = s.Name,
                Location = s.Location,
                ScannerType = s.ScannerType,
                Status = s.Status
            })
            .ToListAsync();
            
        return new ScannerListViewModel { Scanners = scanners };
    }

    public async Task CreateScannerAsync(ScannerCreateViewModel vm, Guid adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = vm.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                Role = Role.SCANNER,
                IsActive = vm.Status == ScannerStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);

            var scanner = new Scanner
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Name = vm.Name,
                Description = vm.Description,
                Location = vm.Location,
                ScannerType = vm.ScannerType,
                Status = vm.Status,
                ApiKey = Guid.NewGuid().ToString("N"),
                ApiKeyIssuedAt = DateTime.UtcNow,
                ApiKeyGeneratedBy = adminId,
                InstalledAt = DateTime.UtcNow,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Scanners.AddAsync(scanner);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ScannerEditViewModel> GetScannerForEditAsync(Guid scannerId)
    {
        var scanner = await _context.Scanners.FirstOrDefaultAsync(s => s.Id == scannerId)
            ?? throw new InvalidOperationException("Scanner not found.");

        return new ScannerEditViewModel
        {
            Id = scanner.Id,
            Name = scanner.Name,
            Description = scanner.Description,
            Location = scanner.Location,
            ScannerType = scanner.ScannerType,
            Status = scanner.Status,
            CurrentApiKey = scanner.ApiKey
        };
    }

    public async Task EditScannerAsync(Guid scannerId, ScannerEditViewModel vm, Guid adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var scanner = await _context.Scanners.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == scannerId)
                ?? throw new InvalidOperationException("Scanner not found.");

            scanner.Name = vm.Name;
            scanner.Description = vm.Description;
            scanner.Location = vm.Location;
            scanner.ScannerType = vm.ScannerType;
            scanner.Status = vm.Status;

            if (scanner.User != null)
            {
                scanner.User.IsActive = vm.Status == ScannerStatus.ACTIVE;
                _context.Users.Update(scanner.User);
            }

            if (vm.RotateApiKey)
            {
                scanner.ApiKey = Guid.NewGuid().ToString("N");
                scanner.ApiKeyIssuedAt = DateTime.UtcNow;
                scanner.ApiKeyGeneratedBy = adminId;
            }

            _context.Scanners.Update(scanner);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task EndAcademicYearAsync(Guid adminId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Expire all ACTIVE devices
            var activeDevices = await _context.Devices.Where(d => d.Status == DeviceStatus.ACTIVE).ToListAsync();
            foreach (var device in activeDevices)
            {
                device.Status = DeviceStatus.EXPIRED;
            }

            // 2. Expire all ACTIVE QR Tokens
            var activeQrTokens = await _context.QRTokens.Where(q => q.Status == TokenStatus.ACTIVE).ToListAsync();
            foreach (var token in activeQrTokens)
            {
                token.Status = TokenStatus.EXPIRED;
            }

            // 3. Expire all ACTIVE RFID Cards
            var activeRfids = await _context.RFIDCards.Where(r => r.Status == TokenStatus.ACTIVE).ToListAsync();
            foreach (var card in activeRfids)
            {
                card.Status = TokenStatus.EXPIRED;
                card.InvalidatedAt = DateTime.UtcNow;
                card.InvalidatedBy = adminId;
                card.InvalidationReason = InvalidationReason.SEMESTER_END;
            }

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