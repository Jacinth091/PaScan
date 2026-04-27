using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models.ViewModels;
using PaScan.Enums;

namespace PaScan.Services.Interfaces;

public interface IAdminService
{
    Task<AdminRequestListViewModel> GetPendingRequestsAsync();
    Task<AdminRequestDetailViewModel> GetRequestDetailAsync(Guid requestId);
    Task ApproveDeviceRequestAsync(Guid requestId, Guid adminId);
    Task RejectDeviceRequestAsync(Guid requestId, Guid adminId, string reason);
    Task<AdminDeviceDetailViewModel> GetDeviceDetailAsync(Guid deviceId);
    Task RevokeQrTokenAsync(Guid deviceId, RevocationReason reason);
    Task RevokeDeviceAsync(Guid deviceId);
    Task<AdminDashboardViewModel> GetDashboardStatsAsync();
    Task<AdminStudentListViewModel> GetAllStudentsAsync();
    Task<AdminDeviceListViewModel> GetAllDevicesAsync();
    Task<AdminRfidListViewModel> GetAllRfidCardsAsync();
    Task<AdminStudentDetailViewModel> GetStudentDetailAsync(Guid studentId);

    Task<ScannerListViewModel> GetAllScannersAsync();
    Task CreateScannerAsync(ScannerCreateViewModel vm, Guid adminId);
    Task<ScannerEditViewModel> GetScannerForEditAsync(Guid scannerId);
    Task EditScannerAsync(Guid scannerId, ScannerEditViewModel vm, Guid adminId);
    
    // System Maintenance
    Task EndAcademicYearAsync(Guid adminId);
}