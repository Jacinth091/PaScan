using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Models.ViewModels;

namespace PaScan.Services.Interfaces;

public interface IAuthService
{
    Task<(User user, string profileId)> AuthenticateStudentAsync(string studentNumber, string password);
    Task<(User user, string profileId)> AuthenticateAdminAsync(string email, string password);
    Task<(User user, string profileId, RefreshToken refreshToken)> AuthenticateScannerAsync(string email, string password);
    Task RegisterStudentAsync(StudentRegisterViewModel model);
    Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string oldRefreshToken);
    Task<List<Course>> GetActiveCoursesAsync();
    string GenerateAccessToken(User user, string profileId);
}