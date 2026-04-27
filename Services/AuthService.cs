using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;
using PaScan.Repositories.Interfaces;
using PaScan.Services.Interfaces;
using BCrypt.Net;

namespace PaScan.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IStudentRepository _studentRepo;
    private readonly IScannerRepository _scannerRepo;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        IStudentRepository studentRepo,
        IScannerRepository scannerRepo,
        IConfiguration configuration)
    {
        _context = context;
        _studentRepo = studentRepo;
        _scannerRepo = scannerRepo;
        _configuration = configuration;
    }

    public async Task<List<Course>> GetActiveCoursesAsync()
    {
        return await _studentRepo.GetActiveCoursesAsync();
    }

    public async Task<(User user, string profileId)> AuthenticateStudentAsync(string studentNumber, string password)
    {
        var user = await _studentRepo.GetStudentUserAsync(studentNumber);
        if (user == null || user.Student == null || !VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid student number or password.");
        }

        return (user, user.Student.Id.ToString());
    }

    public async Task<(User user, string profileId)> AuthenticateAdminAsync(string email, string password)
    {
        var user = await _studentRepo.GetAdminUserAsync(email);
        if (user == null || user.Admin == null || !VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid admin email or password.");
        }

        return (user, user.Admin.Id.ToString());
    }

    public async Task<(User user, string profileId, RefreshToken refreshToken)> AuthenticateScannerAsync(string email, string password)
    {
        var user = await _studentRepo.GetScannerUserAsync(email);
        if (user == null || user.Scanner == null || !VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid scanner email or password.");
        }

        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Expiry = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _scannerRepo.AddRefreshTokenAsync(refreshToken);
        await _context.SaveChangesAsync();

        return (user, user.Scanner.Id.ToString(), refreshToken);
    }

    public async Task RegisterStudentAsync(StudentRegisterViewModel model)
    {
        if (await _studentRepo.IsStudentNumberTakenAsync(model.StudentNumber))
            throw new InvalidOperationException("Student number already registered");

        if (model.Email != null && await _studentRepo.IsEmailTakenAsync(model.Email))
            throw new InvalidOperationException("Email is already taken");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                StudentNumber = model.StudentNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Email = model.Email,
                Role = Role.STUDENT,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _studentRepo.AddUserAsync(user);

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CourseId = model.CourseId,
                StudentNumber = model.StudentNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                Email = model.Email,
                ContactNumber = model.ContactNumber,
                YearLevel = model.YearLevel,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _studentRepo.AddStudentAsync(student);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string oldRefreshToken)
    {
        var token = await _scannerRepo.GetRefreshTokenWithUserAsync(oldRefreshToken);
        if (token == null || token.IsRevoked || token.Expiry < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = token.User;
        var profileId = user.Scanner?.Id.ToString() ?? "";
        var newAccessToken = GenerateAccessToken(user, profileId);
        
        var newRefreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Expiry = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        token.IsRevoked = true;
        await _scannerRepo.UpdateRefreshTokenAsync(token);
        await _scannerRepo.AddRefreshTokenAsync(newRefreshToken);
        await _context.SaveChangesAsync();

        return (newAccessToken, newRefreshToken.Token);
    }

    public string GenerateAccessToken(User user, string profileId)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Key"];
        var key = Encoding.UTF8.GetBytes(jwtKey!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("ProfileId", profileId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        }
        catch
        {
            // Fallback for plain text passwords seeded before hashing was enforced
            return plainPassword == hashedPassword;
        }
    }
}