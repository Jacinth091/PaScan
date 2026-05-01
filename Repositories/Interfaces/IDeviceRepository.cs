using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id);
    Task<Device?> GetByIdWithAccessoriesAndTokenAsync(Guid id);
    Task<List<Device>> GetByStudentIdAsync(Guid studentId);
    Task<List<Device>> GetAllActiveAsync();
    Task<Device?> GetBySerialNumberAndStudentAsync(string serialNumber, Guid studentId);
    Task AddAsync(Device device);
    Task AddAccessoryAsync(DeviceAccessory accessory);
}