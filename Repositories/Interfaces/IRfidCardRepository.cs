using System;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IRfidCardRepository
{
    Task<RFIDCard?> GetActiveByStudentIdAsync(Guid studentId);
    Task<RFIDCard?> GetActiveByCardUidAsync(string cardUid);
    Task AddAsync(RFIDCard card);
    Task UpdateAsync(RFIDCard card);
}