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
using PaScan.Services.Interfaces;

namespace PaScan.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
        try
        {
            var (user, profileId) = await _authService.AuthenticateStudentAsync(studentNumber, password);
            HttpContext.Session.SetString("StudentId", profileId);
            await SignInUser(user, profileId);
            return RedirectToAction("Dashboard", "Student");
        }
        catch (UnauthorizedAccessException ex)
        {
            ViewBag.Error = ex.Message;
            return View("StudentLogin");
        }
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
        try
        {
            var (user, profileId) = await _authService.AuthenticateAdminAsync(email, password);
            HttpContext.Session.SetString("AdminId", profileId);
            await SignInUser(user, profileId);
            return RedirectToAction("Dashboard", "Admin");
        }
        catch (UnauthorizedAccessException ex)
        {
            ViewBag.Error = ex.Message;
            return View("AdminLogin");
        }
    }

    [HttpGet("login/scanner")]
    public IActionResult ScannerLogin()
    {
        return View();
    }

    [HttpPost("login/scanner")]
    public async Task<IActionResult> ScannerLoginPost(string email, string password)
    {
        try
        {
            var (user, profileId, refreshToken) = await _authService.AuthenticateScannerAsync(email, password);
            
            Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.Expiry
            });

            HttpContext.Session.SetString("ScannerId", profileId);
            await SignInUser(user, profileId);
            return RedirectToAction("Dashboard", "Scanner");
        }
        catch (UnauthorizedAccessException ex)
        {
            ViewBag.Error = ex.Message;
            return View("ScannerLogin");
        }
    }

    [HttpGet("register/student")]
    public async Task<IActionResult> StudentRegister()
    {
        var courses = await _authService.GetActiveCoursesAsync();
        ViewBag.Courses = courses;
        return View();
    }

    [HttpPost("register/student")]
    public async Task<IActionResult> StudentRegisterPost(StudentRegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = await _authService.GetActiveCoursesAsync();
            return View("StudentRegister", model);
        }

        try
        {
            await _authService.RegisterStudentAsync(model);
            TempData["SuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("StudentLogin");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Courses = await _authService.GetActiveCoursesAsync();
            return View("StudentRegister", model);
        }
        catch 
        {
            ModelState.AddModelError("", "An error occurred during registration. Please try again.");         
            ViewBag.Courses = await _authService.GetActiveCoursesAsync();
            return View("StudentRegister", model);
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
        var oldRefreshToken = HttpContext.Request.Cookies["refreshToken"];
        if (oldRefreshToken == null) return Unauthorized();

        try
        {
            var (newAccessToken, newRefreshToken) = await _authService.RefreshTokenAsync(oldRefreshToken);

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
