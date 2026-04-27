using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IScannerRepository
{
    Task<Scanner?> GetByUserIdAsync(Guid userId);
    Task<Scanner?> GetByApiKeyAsync(string apiKey);
    Task<Scanner?> GetByIdAsync(Guid id);
    Task<List<Scanner>> GetAllAsync();
    Task AddAsync(Scanner scanner);
    Task UpdateAsync(Scanner scanner);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenWithUserAsync(string token);
    Task UpdateRefreshTokenAsync(RefreshToken token);
}