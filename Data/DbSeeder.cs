using PaScan.Enums;
using PaScan.Models;
using BCrypt.Net;

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

        // Helper to hash passwords consistently
        string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        // Seed Courses
        var courses = new List<Course>
        {
            new Course { Id = Guid.NewGuid(), Code = "BSIT", Name = "Bachelor of Science in Information Technology", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "BSCS", Name = "Bachelor of Science in Computer Science", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "BSIS", Name = "Bachelor of Science in Information Systems", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "BSEMC", Name = "Bachelor of Science in Entertainment and Multimedia Computing", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Course { Id = Guid.NewGuid(), Code = "ACT", Name = "Associate in Computer Technology", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        context.Courses.AddRange(courses);
        context.SaveChanges();

        // Seed Admin User
        var adminUserId = Guid.NewGuid();
        var adminUser = new User
        {
            Id = adminUserId,
            Email = "admin@example.com",
            PasswordHash = HashPassword("admin123"),
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
            Position = "System Administrator",
            Department = "IT Department",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Seed Student 1
        var student1UserId = Guid.NewGuid();
        var student1User = new User
        {
            Id = student1UserId,
            StudentNumber = "23784994",
            PasswordHash = HashPassword("student123"),
            Role = Role.STUDENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var student1 = new Student
        {
            Id = Guid.NewGuid(),
            UserId = student1UserId,
            CourseId = courses[0].Id,
            StudentNumber = "23784994",
            FirstName = "Jacinth Cedric",
            MiddleName = "Curitao",
            LastName = "Barral",
            YearLevel = 3,
            IsRfidEnabled = false,
            RfidRenewalCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Seed Student 2
        var student2UserId = Guid.NewGuid();
        var student2User = new User
        {
            Id = student2UserId,
            StudentNumber = "24001122",
            PasswordHash = HashPassword("student123"),
            Role = Role.STUDENT,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var student2 = new Student
        {
            Id = Guid.NewGuid(),
            UserId = student2UserId,
            CourseId = courses[1].Id,
            StudentNumber = "24001122",
            FirstName = "Elena",
            MiddleName = "Maria",
            LastName = "Santos",
            YearLevel = 2,
            IsRfidEnabled = true,
            RfidRenewalCount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Seed Scanner User
        var scannerUserId = Guid.NewGuid();
        var scannerUser = new User
        {
            Id = scannerUserId,
            Email = "scanner@example.com",
            PasswordHash = HashPassword("scanner123"),
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

        context.Users.AddRange(adminUser, student1User, student2User, scannerUser);
        context.Admins.Add(admin);
        context.Students.AddRange(student1, student2);
        context.Scanners.Add(scanner);

        // --- SEED DEVICES & REQUESTS ---

        // Helper to create a request + device pair for approved ones
        void AddApprovedDevice(Student student, string name, DeviceType type, string brand, string model, string sn, DeviceStatus status = DeviceStatus.ACTIVE)
        {
            var req = new DeviceRequest
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                DeviceName = name,
                DeviceType = type,
                Brand = brand,
                Model = model,
                SerialNumber = sn,
                Purpose = "Educational use",
                Status = RegisterStatus.APPROVED,
                ReviewedBy = admin.Id,
                ReviewedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            context.DeviceRequests.Add(req);

            var dev = new Device
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                OriginalRequestId = req.Id,
                DeviceName = name,
                DeviceType = type,
                Brand = brand,
                Model = model,
                SerialNumber = sn,
                Purpose = "Educational use",
                Status = status,
                ApprovedBy = admin.Id,
                ApprovedAt = DateTime.UtcNow,
                QrRenewalCount = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            context.Devices.Add(dev);

            if (status == DeviceStatus.ACTIVE)
            {
                context.QRTokens.Add(new QRToken
                {
                    Id = Guid.NewGuid(),
                    DeviceId = dev.Id,
                    StudentId = student.Id,
                    TokenValue = Guid.NewGuid().ToString(),
                    IssuedAt = DateTime.UtcNow.AddDays(-10),
                    ExpiresAt = DateTime.UtcNow.AddDays(20),
                    Status = TokenStatus.ACTIVE,
                    RenewalNumber = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                });
            }
        }

        // Student 1 Devices
        AddApprovedDevice(student1, "Pro Laptop", DeviceType.LAPTOP, "Dell", "XPS 15", "SN-DELL-001");
        AddApprovedDevice(student1, "Old MacBook", DeviceType.LAPTOP, "Apple", "MacBook Pro 2015", "SN-MAC-999", DeviceStatus.REVOKED);
        
        context.DeviceRequests.Add(new DeviceRequest {
            StudentId = student1.Id,
            DeviceName = "Development Desktop",
            DeviceType = DeviceType.DESKTOP,
            Brand = "Custom",
            Model = "Ryzen Build",
            SerialNumber = "SN-PC-777",
            Purpose = "Heavy computing",
            Status = RegisterStatus.PENDING,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        // Student 2 Devices
        AddApprovedDevice(student2, "Study Tablet", DeviceType.TABLET, "Apple", "iPad Pro", "SN-IPAD-202");
        AddApprovedDevice(student2, "Windows Laptop", DeviceType.LAPTOP, "HP", "Spectre x360", "SN-HP-555");

        context.DeviceRequests.Add(new DeviceRequest {
            StudentId = student2.Id,
            DeviceName = "Gaming Phone",
            DeviceType = DeviceType.PHONE,
            Brand = "ASUS",
            Model = "ROG Phone",
            SerialNumber = "SN-ROG-888",
            Purpose = "Personal use",
            Status = RegisterStatus.REJECTED,
            ReviewedBy = admin.Id,
            ReviewedAt = DateTime.UtcNow.AddDays(-5),
            RejectionReason = "Personal mobile devices do not require campus registration tags.",
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        context.DeviceRequests.Add(new DeviceRequest {
            StudentId = student2.Id,
            DeviceName = "AI Workstation",
            DeviceType = DeviceType.DESKTOP,
            Brand = "Lenovo",
            Model = "ThinkStation",
            SerialNumber = "SN-LEN-444",
            Purpose = "AI Research",
            Status = RegisterStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
    }
}
