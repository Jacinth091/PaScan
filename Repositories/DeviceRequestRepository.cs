using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PaScan.Repositories;

public class DeviceRequestRepository : IDeviceRequestRepository
{
    private readonly AppDbContext _context;
    public DeviceRequestRepository(AppDbContext context) => _context = context;

    public async Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id)
    {
        return await _context.DeviceRequests
            .Include(r => r.Accessories)
            .Include(r => r.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<DeviceRequest>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.DeviceRequests
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DeviceRequest>> GetAllPendingAsync()
    {
        return await _context.DeviceRequests
            .Include(r => r.Student)
            .Where(r => r.Status == Enums.RegisterStatus.PENDING)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(DeviceRequest request)
    {
        await _context.DeviceRequests.AddAsync(request);
    }

    public async Task AddAccessoryAsync(DeviceRequestAccessory accessory)
    {
        await _context.DeviceRequestAccessories.AddAsync(accessory);
    }
}