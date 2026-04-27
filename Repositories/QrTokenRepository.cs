using System;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;

namespace PaScan.Repositories;

public class QrTokenRepository : IQrTokenRepository
{
    private readonly AppDbContext _context;
    public QrTokenRepository(AppDbContext context) => _context = context;

    public async Task<QRToken?> GetActiveByDeviceIdAsync(Guid deviceId)
    {
        return await _context.QRTokens
            .FirstOrDefaultAsync(t => t.DeviceId == deviceId && t.Status == Enums.TokenStatus.ACTIVE);
    }

    public async Task<QRToken?> GetByTokenValueAsync(string tokenValue)
    {
        return await _context.QRTokens
            .FirstOrDefaultAsync(t => t.TokenValue == tokenValue);
    }

    public async Task AddAsync(QRToken token)
    {
        await _context.QRTokens.AddAsync(token);
    }

    public async Task UpdateAsync(QRToken token)
    {
        _context.QRTokens.Update(token);
        await Task.CompletedTask;
    }
}