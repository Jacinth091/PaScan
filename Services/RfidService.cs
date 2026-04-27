using System;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Enums;
using PaScan.Services.Interfaces;
using PaScan.Repositories.Interfaces;
using PaScan.Data;

using System.Collections.Concurrent;

namespace PaScan.Services;

public class RfidService : IRfidService
{
    private readonly AppDbContext _context;
    private readonly IStudentRepository _studentRepo;
    private readonly IRfidCardRepository _rfidCardRepo;
    
    // Static storage for recent scans (DeviceId -> CardUid)
    private static readonly ConcurrentDictionary<string, string> _lastScans = new(StringComparer.OrdinalIgnoreCase);
    
    // Static storage for Admin "Waiting" sessions (DeviceId -> Expiration)
    private static readonly ConcurrentDictionary<string, DateTime> _waitingAdmins = new(StringComparer.OrdinalIgnoreCase);

    public RfidService(AppDbContext context, IStudentRepository studentRepo, IRfidCardRepository rfidCardRepo)
    {
        _context = context;
        _studentRepo = studentRepo;
        _rfidCardRepo = rfidCardRepo;
    }

    public void FlagAdminWaiting(string deviceId)
    {
        // Admin is waiting for the next 2 minutes
        _waitingAdmins[deviceId] = DateTime.UtcNow.AddMinutes(2);
    }

    public bool IsAdminWaiting(string deviceId)
    {
        if (_waitingAdmins.TryGetValue(deviceId, out var expiry))
        {
            if (expiry > DateTime.UtcNow) return true;
            _waitingAdmins.TryRemove(deviceId, out _); // Clean up expired
        }
        return false;
    }

    public void RegisterScan(string cardUid, string deviceId)
    {
        _lastScans[deviceId] = cardUid;
        _waitingAdmins.TryRemove(deviceId, out _); // Once scanned, they aren't waiting anymore
    }

    public string? GetLastScan(string deviceId)
    {
        if (_lastScans.TryRemove(deviceId, out var uid))
        {
            return uid;
        }
        return null;
    }

    public async Task IssueCardAsync(Guid studentId, Guid adminId, string cardUid, DateTime semesterExpiresAt)
    {
        var student = await _studentRepo.GetByIdAsync(studentId)
            ?? throw new InvalidOperationException("Student not found.");

        var existing = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId);
        if (existing != null)
            throw new InvalidOperationException("Student already has an active RFID card.");

        var cardInUse = await _rfidCardRepo.GetActiveByCardUidAsync(cardUid);
        if (cardInUse != null)
            throw new InvalidOperationException("This RFID card is already assigned to another student.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _rfidCardRepo.AddAsync(new RFIDCard
            {
                StudentId         = studentId,
                CardUid           = cardUid,
                IssuedAt          = DateTime.UtcNow,
                SemesterExpiresAt = semesterExpiresAt,
                Status            = TokenStatus.ACTIVE,
                RenewalNumber     = 0,
                IsReplacement     = false,
                IssuedBy          = adminId,
                CreatedAt         = DateTime.UtcNow
            });

            student.IsRfidEnabled   = true;
            student.RfidRenewalCount = 0;
            student.UpdatedAt       = DateTime.UtcNow;
            await _studentRepo.UpdateAsync(student);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RenewSemesterAsync(Guid studentId, Guid adminId, DateTime newExpiresAt)
    {
        var oldCard = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId)
            ?? throw new InvalidOperationException("No active RFID card found for this student.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            oldCard.Status = TokenStatus.EXPIRED;
            oldCard.InvalidatedAt = DateTime.UtcNow;
            oldCard.InvalidatedBy = adminId;
            oldCard.InvalidationReason = InvalidationReason.SEMESTER_END;
            oldCard.UpdatedAt = DateTime.UtcNow;
            await _rfidCardRepo.UpdateAsync(oldCard);

            var newCard = new RFIDCard
            {
                StudentId = studentId,
                CardUid = oldCard.CardUid,
                IssuedAt = DateTime.UtcNow,
                SemesterExpiresAt = newExpiresAt,
                Status = TokenStatus.ACTIVE,
                RenewalNumber = oldCard.RenewalNumber + 1,
                RenewedFrom = oldCard.Id,
                IsReplacement = false,
                IssuedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };
            await _rfidCardRepo.AddAsync(newCard);

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student != null)
            {
                student.RfidRenewalCount += 1;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepo.UpdateAsync(student);
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

    public async Task ReplaceCardAsync(Guid studentId, Guid adminId, string newCardUid, InvalidationReason reason)
    {
        var oldCard = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId)
            ?? throw new InvalidOperationException("No active RFID card found for this student.");

        if (reason == InvalidationReason.SEMESTER_END)
            throw new InvalidOperationException("For semester end, use Renew Semester instead of Replace Card.");

        var cardInUse = await _rfidCardRepo.GetActiveByCardUidAsync(newCardUid);
        if (cardInUse != null)
            throw new InvalidOperationException("This RFID card is already assigned to another student.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            oldCard.Status = TokenStatus.REVOKED;
            oldCard.InvalidatedAt = DateTime.UtcNow;
            oldCard.InvalidatedBy = adminId;
            oldCard.InvalidationReason = reason;
            oldCard.UpdatedAt = DateTime.UtcNow;
            await _rfidCardRepo.UpdateAsync(oldCard);

            var newCard = new RFIDCard
            {
                StudentId = studentId,
                CardUid = newCardUid,
                IssuedAt = DateTime.UtcNow,
                SemesterExpiresAt = oldCard.SemesterExpiresAt, // Keep the same expiry
                Status = TokenStatus.ACTIVE,
                RenewalNumber = oldCard.RenewalNumber + 1,
                RenewedFrom = oldCard.Id,
                IsReplacement = true,
                PreviousCardUid = oldCard.CardUid,
                IssuedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };
            await _rfidCardRepo.AddAsync(newCard);

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student != null)
            {
                student.RfidRenewalCount += 1;
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepo.UpdateAsync(student);
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

    public async Task RevokeCardAsync(Guid studentId, Guid adminId, InvalidationReason reason)
    {
        var oldCard = await _rfidCardRepo.GetActiveByStudentIdAsync(studentId)
            ?? throw new InvalidOperationException("No active RFID card found for this student.");

        if (reason == InvalidationReason.SEMESTER_END)
            throw new InvalidOperationException("For semester end, use Renew Semester instead of Revoke Card.");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            oldCard.Status = TokenStatus.REVOKED;
            oldCard.InvalidatedAt = DateTime.UtcNow;
            oldCard.InvalidatedBy = adminId;
            oldCard.InvalidationReason = reason;
            oldCard.UpdatedAt = DateTime.UtcNow;
            await _rfidCardRepo.UpdateAsync(oldCard);

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student != null)
            {
                student.IsRfidEnabled = false; // Disable RFID access for this student
                student.UpdatedAt = DateTime.UtcNow;
                await _studentRepo.UpdateAsync(student);
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