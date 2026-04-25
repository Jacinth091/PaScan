using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaScan.Data;
using PaScan.Enums;
using PaScan.Models;
using PaScan.Models.ViewModels;

namespace PaScan.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("login")]
    public IActionResult LoginSelector()
    {
        return View();
    }

    [HttpGet("login/student")]
    public IActionResult StudentLogin()
    {
        return View();
    }

    [HttpPost("login/student")]
    public async Task<IActionResult> StudentLoginPost(string studentNumber, string password)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.StudentNumber == studentNumber && u.Role == Role.STUDENT && u.PasswordHash == password);

        if (user == null || user.Student == null)
        {
            ViewBag.Error = "Invalid student number or password.";
            return View("StudentLogin");
        }

        HttpContext.Session.SetString("StudentId", user.Student.Id.ToString());
        await SignInUser(user, user.Student.Id.ToString());
        return RedirectToAction("Dashboard", "Student");
    }

    [HttpGet("login/admin")]
    public IActionResult AdminLogin()
    {
        return View();
    }

    [HttpPost("login/admin")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AdminLoginPost(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Admin)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.ADMIN && u.PasswordHash == password);

        if (user == null || user.Admin == null)
        {
            ViewBag.Error = "Invalid admin email or password.";
            return View("AdminLogin");
        }

        HttpContext.Session.SetString("AdminId", user.Admin.Id.ToString());
        await SignInUser(user, user.Admin.Id.ToString());
        return RedirectToAction("Dashboard", "Admin");
    }

    [HttpGet("login/scanner")]
    public IActionResult ScannerLogin()
    {
        return View();
    }

    [HttpPost("login/scanner")]
    public async Task<IActionResult> ScannerLoginPost(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Scanner)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.SCANNER && u.PasswordHash == password);

        if (user == null || user.Scanner == null)
        {
            ViewBag.Error = "Invalid scanner email or password.";
            return View("ScannerLogin");
        }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.Expiry
        });

        HttpContext.Session.SetString("ScannerId", user.Scanner.Id.ToString());
        await SignInUser(user, user.Scanner.Id.ToString());
        return RedirectToAction("QrScanPage", "Scan");
    }

    [HttpGet("register/student")]
    public async Task<IActionResult> StudentRegister()
    {
        var courses = await _context.Courses.Where(c => c.DeletedAt == null).ToListAsync();
        ViewBag.Courses = courses;
        return View();
    }

    [HttpPost("register/student")]
    public async Task<IActionResult> StudentRegisterPost(StudentRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var courses = await _context.Courses.Where(c => c.DeletedAt == null).ToListAsync();
            ViewBag.Courses = courses;
            return View("StudentRegister", model);
        }

        if(await _context.Users.AnyAsync(u => u.StudentNumber == model.StudentNumber))
        {
            ModelState.AddModelError("StudentNumber", "Student number already registered");
            var courses = await _context.Courses.Where(c => c.DeletedAt == null).ToListAsync();
            ViewBag.Courses = courses;
            return View("StudentRegister", model);
        }

        if(await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Email is already taken");
            var courses = await _context.Courses.Where(c => c.DeletedAt == null).ToListAsync();
            ViewBag.Courses = courses;
            return View("StudentRegister", model);
        }
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
                CreatedAt = DateTime.Now,
            };
            _context.Users.Add(user);

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
                CreatedAt = DateTime.Now,
            };
            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("StudentLogin");
        }
        catch 
        {
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");         
            return View(model);
        }
    }

    private async Task SignInUser(User user, string profileId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("ProfileId", profileId)
        };

        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);
    }

    private string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSettings["Key"];
        var key = Encoding.UTF8.GetBytes(jwtKey!);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("ProfileId", user.Scanner?.Id.ToString() ?? "")
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

    private RefreshToken GenerateRefreshToken(Guid userId)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Expiry = DateTime.UtcNow.AddDays(7),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("Cookies");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];
        if (refreshToken == null) return Unauthorized();

        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.IsRevoked || token.Expiry < DateTime.UtcNow) return Unauthorized();

        var user = token.User;
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken(user.Id);

        token.IsRevoked = true;
        _context.RefreshTokens.Update(token);
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", newRefreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = newRefreshToken.Expiry
        });

        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken.Token });
    }
}
