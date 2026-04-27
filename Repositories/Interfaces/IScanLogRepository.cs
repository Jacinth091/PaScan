using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IScanLogRepository
{
    Task AddAsync(GateScanLog log);
    Task AddRangeAsync(List<GateScanLog> logs);
    Task<List<GateScanLog>> GetRecentAsync(int count);
}