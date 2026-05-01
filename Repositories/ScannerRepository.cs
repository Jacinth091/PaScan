using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;

namespace PaScan.Repositories;

public class ScannerRepository : IScannerRepository
{
    private readonly AppDbContext _context;
    public ScannerRepository(AppDbContext context) => _context = context;

    public async Task<Scanner?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Scanners
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<Scanner?> GetByApiKeyAsync(string apiKey)
    {
        return await _context.Scanners
            .FirstOrDefaultAsync(s => s.ApiKey == apiKey);
    }

    public async Task<Scanner?> GetByIdAsync(Guid id)
    {
        return await _context.Scanners
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Scanner>> GetAllAsync()
    {
        return await _context.Scanners.ToListAsync();
    }

    public async Task AddAsync(Scanner scanner)
    {
        await _context.Scanners.AddAsync(scanner);
    }

    public async Task UpdateAsync(Scanner scanner)
    {
        _context.Scanners.Update(scanner);
        await Task.CompletedTask;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
    }

    public async Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public Task UpdateRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }
}