using PaScan.Models;
using PaScan.Enums;

namespace PaScan.Data;

public static class DbSeeder
{
    public static void Initialize(AppDbContext context)
    {
        // Check if database already has courses
        if (context.Courses.Any())
        {
            return; // DB has been seeded
        }

        // Seed Courses
        var courses = new List<Course>
        {
            new Course { Id = Guid.NewGuid(), Code = "CS101", Name = "Intro to Computer Science", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "IT201", Name = "Information Technology Basics", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "SE301", Name = "Software Engineering", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        // Seed Admin User
        var adminUserId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminUserId,
            Email = "admin@pascan.edu",
            PasswordHash = "admin123", // In actual prod, hash this!
            Role = Role.ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            UserId = adminUserId,
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Seed Student User
        var studentUserId = Guid.NewGuid();
        var studentUser = new User
        {
            Id = studentUserId,
            StudentNumber = "20230001",
            PasswordHash = "student123",
            Role = Role.STUDENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var student = new Student
        {
            Id = Guid.NewGuid(),
            UserId = studentUserId,
            CourseId = courses[0].Id,
            StudentNumber = "20230001",
            FirstName = "John",
            LastName = "Doe",
            YearLevel = 1,
            IsRfidEnabled = false,
            RfidRenewalCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Seed Scanner User
        var scannerUserId = Guid.NewGuid();
        var scannerUser = new User
        {
            Id = scannerUserId,
            Email = "gate1@pascan.edu",
            PasswordHash = "scanner123",
            Role = Role.SCANNER,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var scanner = new Scanner
        {
            Id = Guid.NewGuid(),
            UserId = scannerUserId,
            Name = "Main Gate QR",
            Location = "North Entrance",
            ScannerType = ScannerType.QR,
            Status = ScannerStatus.ACTIVE,
            CreatedBy = admin.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(adminUser, studentUser, scannerUser);
        context.Admins.Add(admin);
        context.Students.Add(student);
        context.Scanners.Add(scanner);

        context.SaveChanges();
    }
}
