using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaScan.Data;
using PaScan.Models;
using PaScan.Enums;


namespace PaScan.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }


    [HttpGet]
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
    public async Task<IActionResult> StudentLogin(string studentNumber, string password)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.StudentNumber == studentNumber && u.Role == Role.STUDENT && u.PasswordHash == password);

        if (user == null || user.Student == null)
        {
            ViewBag.Error = "Invalid student number or password.";
            return View();
        }

        await SignInUser(user, user.Student.Id.ToString());
        return RedirectToAction("Dashboard", "Student");
    }

    [HttpGet("login/admin")]
    public IActionResult AdminLogin()
    {
        return View();
    }

    [HttpPost("login/admin")]
    public async Task<IActionResult> AdminLogin(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Admin)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.ADMIN && u.PasswordHash == password);

        if (user == null || user.Admin == null)
        {
            ViewBag.Error = "Invalid admin email or password.";
            return View();
        }

        await SignInUser(user, user.Admin.Id.ToString());
        return RedirectToAction("Dashboard", "Admin");
    }

    [HttpGet("login/scanner")]
    public IActionResult ScannerLogin()
    {
        return View();
    }

    [HttpPost("login/scanner")]
    public async Task<IActionResult> ScannerLogin(string email, string password)
    {
        var user = await _context.Users
            .Include(u => u.Scanner)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.SCANNER && u.PasswordHash == password);

        if (user == null || user.Scanner == null)
        {
            ViewBag.Error = "Invalid scanner email or password.";
            return View();
        }

        await SignInUser(user, user.Scanner.Id.ToString());
        return RedirectToAction("QrScanPage", "Scan");
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

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
