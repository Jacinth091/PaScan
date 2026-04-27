using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PaScan.Models;
using PaScan.Repositories.Interfaces;
using PaScan.Data;
using Microsoft.EntityFrameworkCore;
using PaScan.Enums;

namespace PaScan.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _context;
    public StudentRepository(AppDbContext context) => _context = context;

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        return await _context.Students.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Student?> GetByIdWithUserAsync(Guid id)
    {
        return await _context.Students
            .Include(s => s.User)
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Student>> GetAllAsync()
    {
        return await _context.Students.ToListAsync();
    }

    public Task UpdateAsync(Student student)
    {
        _context.Students.Update(student);
        return Task.CompletedTask;
    }

    public async Task<User?> GetStudentUserAsync(string studentNumber)
    {
        return await _context.Users
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.StudentNumber == studentNumber && u.Role == Role.STUDENT);
    }

    public async Task<User?> GetAdminUserAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Admin)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.ADMIN);
    }

    public async Task<User?> GetScannerUserAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Scanner)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == Role.SCANNER);
    }

    public async Task<bool> IsStudentNumberTakenAsync(string studentNumber)
    {
        return await _context.Users.AnyAsync(u => u.StudentNumber == studentNumber);
    }

    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task AddStudentAsync(Student student)
    {
        await _context.Students.AddAsync(student);
    }

    public async Task<List<Course>> GetActiveCoursesAsync()
    {
        return await _context.Courses.Where(c => c.DeletedAt == null).ToListAsync();
    }
}