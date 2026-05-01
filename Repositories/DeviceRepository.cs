using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Models;
using PaScan.Repositories.Interfaces;

namespace PaScan.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _context;
    public DeviceRepository(AppDbContext context) => _context = context;

    public async Task<Device?> GetByIdAsync(Guid id)
    {
        return await _context.Devices.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Device?> GetByIdWithAccessoriesAndTokenAsync(Guid id)
    {
        return await _context.Devices
            .Include(d => d.Accessories)
            .Include(d => d.QRTokens.Where(t => t.Status == Enums.TokenStatus.ACTIVE))
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Device>> GetByStudentIdAsync(Guid studentId)
    {
        return await _context.Devices
            .Where(d => d.StudentId == studentId)
            .ToListAsync();
    }


    public async Task<List<Device>> GetAllActiveAsync()
    {
        return await _context.Devices
            .Where(d => d.Status == Enums.DeviceStatus.ACTIVE)
            .ToListAsync();
    }

    public async Task<Device?> GetBySerialNumberAndStudentAsync(string serialNumber, Guid studentId)
    {
        return await _context.Devices
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber && d.StudentId == studentId);
    }

    public async Task AddAsync(Device device)
    {
        await _context.Devices.AddAsync(device);
    }

    public async Task AddAccessoryAsync(DeviceAccessory accessory)
    {
        await _context.DeviceAccessories.AddAsync(accessory);
    }
}