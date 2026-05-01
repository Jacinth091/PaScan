using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PaScan.Repositories;

public class ScanLogRepository : IScanLogRepository
{
    private readonly AppDbContext _context;
    public ScanLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(GateScanLog log)
    {
        await _context.GateScanLogs.AddAsync(log);
    }

    public async Task AddRangeAsync(List<GateScanLog> logs)
    {
        await _context.GateScanLogs.AddRangeAsync(logs);
    }

    public async Task<List<GateScanLog>> GetRecentAsync(int count)
    {
        return await _context.GateScanLogs
            .OrderByDescending(l => l.ScannedAt)
            .Take(count)
            .ToListAsync();
    }
}