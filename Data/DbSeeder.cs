using PaScan.Enums;
using PaScan.Models;

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
            StudentNumber = "23784994",
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

        // Seed Device Requests
        var laptopRequestId = Guid.NewGuid();
        var laptopRequest = new DeviceRequest
        {
            Id = laptopRequestId,
            StudentId = student.Id,
            DeviceName = "Academic Laptop",
            DeviceType = DeviceType.LAPTOP,
            Brand = "Dell",
            Model = "XPS 15",
            SerialNumber = "DELL-XPS-98765",
            Purpose = "Primary device for programming and research",
            OperatingSystem = "Windows 11 Pro",
            Processor = "Intel Core i9-13900H",
            Memory = "32GB DDR5",
            Storage = "1TB NVMe SSD",
            Status = RegisterStatus.APPROVED,
            ReviewedBy = admin.Id,
            ReviewedAt = DateTime.UtcNow.AddDays(-5),
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var tabletRequestId = Guid.NewGuid();
        var tabletRequest = new DeviceRequest
        {
            Id = tabletRequestId,
            StudentId = student.Id,
            DeviceName = "Study Tablet",
            DeviceType = DeviceType.TABLET,
            Brand = "Apple",
            Model = "iPad Air",
            SerialNumber = "IPAD-AIR-54321",
            Purpose = "Note-taking and reading digital textbooks",
            Color = "Space Gray",
            Storage = "256GB",
            Status = RegisterStatus.PENDING,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var rejectedRequestId = Guid.NewGuid();
        var rejectedRequest = new DeviceRequest
        {
            Id = rejectedRequestId,
            StudentId = student.Id,
            DeviceName = "Personal Phone",
            DeviceType = DeviceType.PHONE,
            Brand = "Samsung",
            Model = "S23 Ultra",
            SerialNumber = "SAMSUNG-S23-000",
            Purpose = "Personal communication",
            Status = RegisterStatus.REJECTED,
            ReviewedBy = admin.Id,
            ReviewedAt = DateTime.UtcNow.AddDays(-2),
            RejectionReason = "Personal mobile phones do not require campus registration.",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        context.DeviceRequests.AddRange(laptopRequest, tabletRequest, rejectedRequest);

        // Seed Approved Device
        var approvedDevice = new Device
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            OriginalRequestId = laptopRequestId,
            DeviceName = "Academic Laptop",
            DeviceType = DeviceType.LAPTOP,
            Brand = "Dell",
            Model = "XPS 15",
            SerialNumber = "DELL-XPS-98765",
            Purpose = "Primary device for programming and research",
            OperatingSystem = "Windows 11 Pro",
            Processor = "Intel Core i9-13900H",
            Memory = "32GB DDR5",
            Storage = "1TB NVMe SSD",
            Status = DeviceStatus.ACTIVE,
            ApprovedBy = admin.Id,
            ApprovedAt = DateTime.UtcNow.AddDays(-5),
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.Devices.Add(approvedDevice);

        // Seed Accessories
        var charger = new DeviceAccessory
        {
            Id = Guid.NewGuid(),
            DeviceId = approvedDevice.Id,
            AccessoryName = "130W USB-C Charger",
            Quantity = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var mouse = new DeviceAccessory
        {
            Id = Guid.NewGuid(),
            DeviceId = approvedDevice.Id,
            AccessoryName = "Wireless Mouse",
            Quantity = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.DeviceAccessories.AddRange(charger, mouse);

        // Seed QR Token for the approved device
        var qrToken = new QRToken
        {
            Id = Guid.NewGuid(),
            DeviceId = approvedDevice.Id,
            StudentId = student.Id,
            TokenValue = $"PASCAN-{approvedDevice.SerialNumber}-{Guid.NewGuid().ToString().Substring(0, 8)}",
            IssuedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(25),
            Status = TokenStatus.ACTIVE,
            RenewalNumber = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.QRTokens.Add(qrToken);

        context.SaveChanges();
    }
}
