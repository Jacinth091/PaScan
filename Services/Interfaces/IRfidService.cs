using System;
using System.Threading.Tasks;
using PaScan.Enums;

namespace PaScan.Services.Interfaces;

public interface IRfidService
{
    Task IssueCardAsync(Guid studentId, Guid adminId, string cardUid, DateTime semesterExpiresAt);  // T2
    Task RenewSemesterAsync(Guid studentId, Guid adminId, DateTime newExpiresAt);                   // T6 — bonus
    Task ReplaceCardAsync(Guid studentId, Guid adminId, string newCardUid, InvalidationReason reason); // T7 — bonus
    Task RevokeCardAsync(Guid studentId, Guid adminId, InvalidationReason reason);
    
    // For integration with bridge app
    void RegisterScan(string cardUid, string deviceId);
    string? GetLastScan(string deviceId);
    
    // Smart Intercept logic
    void FlagAdminWaiting(string deviceId);
    bool IsAdminWaiting(string deviceId);
}