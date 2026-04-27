using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IDeviceRequestRepository
{
    Task<DeviceRequest?> GetByIdWithAccessoriesAsync(Guid id);
    Task<List<DeviceRequest>> GetByStudentIdAsync(Guid studentId);
    Task<List<DeviceRequest>> GetAllPendingAsync();
    Task AddAsync(DeviceRequest request);
    Task AddAccessoryAsync(DeviceRequestAccessory accessory);
}