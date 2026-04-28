using System;
using System.Threading.Tasks;
using PaScan.Models.ViewModels;

namespace PaScan.Services.Interfaces;

public interface IDeviceService
{
    Task<StudentDashboardViewModel> GetStudentDashboardAsync(Guid studentId);
    Task<DeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId, Guid studentId);
    Task SubmitDeviceRequestAsync(DeviceRequestViewModel vm, Guid studentId);
    Task RenewQrAsync(Guid deviceId, Guid studentId);  // T5 — bonus
    Task<PaScan.Models.DeviceRequest> GetDeviceRequestAsync(Guid requestId, Guid studentId);
}