using System;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;

namespace PaScan.Repositories;

public class RfidCardRepository : IRfidCardRepository
{
    private readonly AppDbContext _context;
    public RfidCardRepository(AppDbContext context) => _context = context;

    public async Task<RFIDCard?> GetActiveByStudentIdAsync(Guid studentId)
    {
        return await _context.RFIDCards
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Status == Enums.TokenStatus.ACTIVE);
    }

    public async Task<RFIDCard?> GetActiveByCardUidAsync(string cardUid)
    {
        return await _context.RFIDCards
            .FirstOrDefaultAsync(c => c.CardUid == cardUid && c.Status == Enums.TokenStatus.ACTIVE);
    }

    public async Task AddAsync(RFIDCard card)
    {
        await _context.RFIDCards.AddAsync(card);
    }

    public async Task UpdateAsync(RFIDCard card)
    {
        _context.RFIDCards.Update(card);
        await Task.CompletedTask;
    }
}