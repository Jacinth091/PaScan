using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaScan.Models;

namespace PaScan.Repositories.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<Student?> GetByIdWithUserAsync(Guid id);
    Task<List<Student>> GetAllAsync();
    Task UpdateAsync(Student student);
    Task<User?> GetStudentUserAsync(string studentNumber);
    Task<User?> GetAdminUserAsync(string email);
    Task<User?> GetScannerUserAsync(string email);
    Task<bool> IsStudentNumberTakenAsync(string studentNumber);
    Task<bool> IsEmailTakenAsync(string email);
    Task AddUserAsync(User user);
    Task AddStudentAsync(Student student);
    Task<List<Course>> GetActiveCoursesAsync();
}