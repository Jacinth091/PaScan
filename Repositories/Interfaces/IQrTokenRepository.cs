using System;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IQrTokenRepository
{
    Task<QRToken?> GetActiveByDeviceIdAsync(Guid deviceId);
    Task<QRToken?> GetByTokenValueAsync(string tokenValue);
    Task AddAsync(QRToken token);
    Task UpdateAsync(QRToken token);
}