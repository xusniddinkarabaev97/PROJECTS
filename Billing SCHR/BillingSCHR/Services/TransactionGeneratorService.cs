using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ODULink.Data;
using ODULink.Enums;
using ODULink.Models;

namespace ODULink.Services;

/// <summary>
/// Фоновый сервис генерации случайных транзакций каждые 15 минут
/// </summary>
public class TransactionGeneratorService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<TransactionGeneratorService> _logger;
    private static readonly Random _rng = new();

    private static readonly string[] FirstNames = ["Алексей", "Дмитрий", "Сергей", "Андрей", "Максим", "Руслан", "Тимур", "Бахтиёр", "Шухрат", "Азиз",
        "Елена", "Ольга", "Татьяна", "Марина", "Анна", "Гульнара", "Дильноза", "Феруза", "Нодира", "Малика"];
    private static readonly string[] LastNames = ["Иванов", "Петров", "Сидоров", "Кузнецов", "Смирнов", "Каримов", "Алиев", "Усманов", "Рахимов", "Назаров",
        "Иванова", "Петрова", "Сидорова", "Кузнецова", "Смирнова", "Каримова", "Алиева", "Усманова", "Рахимова", "Назарова"];
    private static readonly string[] Ranks = ["Рядовой", "Сержант", "Лейтенант", "Капитан", "Майор", "Подполковник", "Полковник"];
    private static readonly string[] Units = ["В/Ч 12345", "В/Ч 67890", "В/Ч 11111", "В/Ч 22222", "В/Ч 33333"];
    private static readonly string[] BloodTypes = ["I+", "II+", "III+", "IV+", "I-", "II-"];
    private static readonly string[] Doctors = ["Иванов А.В.", "Петров С.Н.", "Саидов Р.К.", "Каримов Д.М.", "Алиев Б.Т.",
        "Мирзаева Г.Р.", "Ахмедов Ш.К.", "Юсупова Н.Ф."];
    private static readonly string[] Diagnoses = ["Острый бронхит", "Гипертония I ст.", "Язва желудка", "Сахарный диабет 2 типа",
        "Остеохондроз", "Мигрень", "Пневмония", "Гастрит", "Ангина", "Перелом предплечья"];
    private static readonly string[] Treatments = ["Медикаментозная терапия", "Физиотерапия", "Хирургическое вмешательство",
        "ЛФК и массаж", "Стационарное наблюдение", "Амбулаторное лечение", "Ингаляции", "Инъекции антибиотиков"];
    private static readonly string[] PaymentMethods = ["cash", "uzcard", "humo", "payme", "click", "transfer"];
    private static readonly string[] Statuses = ["medical", "laboratory", "procedure", "pharmacy"];
    private static readonly string[] DeptNames = ["Хирургия", "Терапия", "Кардиология", "Реанимация", "Лаборатория",
        "Стоматология", "МРТ/КТ", "Травматология", "Неврология", "Урология"];

    public TransactionGeneratorService(IServiceProvider sp, ILogger<TransactionGeneratorService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Генератор транзакций запущен. Интервал: 15 минут.");

        // Первая генерация через 5 секунд после старта
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateTransactions();
                _logger.LogInformation("Транзакции сгенерированы. Следующая генерация через 15 минут.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации транзакций");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task GenerateTransactions()
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure departments exist
        await EnsureDepartments(db);

        // Ensure patients exist (at least 5)
        await EnsurePatients(db);

        var patients = await db.Patients.ToListAsync();
        var departments = await db.Departments.ToListAsync();
        var company = await db.Companies.FirstOrDefaultAsync();

        if (patients.Count == 0) return;

        // Generate 2-6 random transactions
        int count = _rng.Next(2, 7);
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var patient = patients[_rng.Next(patients.Count)];
            var dept = departments.Count > 0 ? departments[_rng.Next(departments.Count)] : null;
            var paymentMethod = PaymentMethods[_rng.Next(PaymentMethods.Length)];
            
            // 80% Completed, 15% Pending, 5% Failed
            var statusRoll = _rng.Next(100);
            var paymentStatus = statusRoll < 80 ? PaymentStatus.Completed
                : statusRoll < 95 ? PaymentStatus.Pending
                : PaymentStatus.Failed;

            // Random time within last 2 hours
            var filledAt = now.AddMinutes(-_rng.Next(0, 120));

            var tx = new Transaction
            {
                PatientId = patient.Id,
                DepartmentId = dept?.Id,
                Diagnosis = Diagnoses[_rng.Next(Diagnoses.Length)],
                TreatmentDescription = Treatments[_rng.Next(Treatments.Length)],
                DoctorName = Doctors[_rng.Next(Doctors.Length)],
                TotalSum = Math.Round((decimal)(_rng.Next(50000, 500000) / 1000) * 1000, 0),
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                FilledAt = filledAt,
                Status = Statuses[_rng.Next(Statuses.Length)],
                CompanyId = company?.Id
            };

            db.Transactions.Add(tx);
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsureDepartments(ApplicationDbContext db)
    {
        if (await db.Departments.AnyAsync()) return;

        var company = await db.Companies.FirstOrDefaultAsync();
        if (company == null) return;

        var depts = DeptNames.Select(name => new Department
        {
            Name = name,
            CompanyId = company.Id,
            DepartmentType = name switch
            {
                "Хирургия" => "surgery",
                "Терапия" => "therapy",
                "Кардиология" => "cardiology",
                "Реанимация" => "icu",
                "Лаборатория" => "laboratory",
                "Стоматология" => "dentistry",
                "МРТ/КТ" => "diagnostics",
                "Травматология" => "traumatology",
                "Неврология" => "neurology",
                _ => "general"
            },
            Location = $"Корпус {_rng.Next(1, 5)}, этаж {_rng.Next(1, 5)}",
            HeadDoctor = Doctors[_rng.Next(Doctors.Length)],
            BedCount = _rng.Next(10, 60),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        db.Departments.AddRange(depts);
        await db.SaveChangesAsync();
        _logger.LogInformation("Создано {Count} отделений", depts.Count);
    }

    private async Task EnsurePatients(ApplicationDbContext db)
    {
        var currentCount = await db.Patients.CountAsync();
        int toCreate = Math.Max(0, 15 - currentCount);
        if (toCreate == 0) return;

        var patients = new List<Patient>();
        for (int i = 0; i < toCreate; i++)
        {
            var lastName = LastNames[_rng.Next(LastNames.Length)];
            var firstName = FirstNames[_rng.Next(FirstNames.Length)];
            
            patients.Add(new Patient
            {
                ExternalId = $"EXT-{DateTime.UtcNow:yyyyMMdd}-{_rng.Next(1000, 9999)}",
                FullName = $"{lastName} {firstName} {(_rng.Next(0, 2) == 0 ? "Александрович" : "Владимирович")}",
                MilitaryRank = Ranks[_rng.Next(Ranks.Length)],
                MilitaryUnit = Units[_rng.Next(Units.Length)],
                PersonalNumber = $"PN-{_rng.Next(100000, 999999)}",
                Phone = $"+9989{_rng.Next(0, 10)}{_rng.Next(10000000, 99999999)}",
                BirthDate = new DateTime(_rng.Next(1975, 2005), _rng.Next(1, 13), _rng.Next(1, 28)),
                BloodType = BloodTypes[_rng.Next(BloodTypes.Length)],
                IsVerified = true,
                RegisteredAt = DateTime.UtcNow.AddDays(-_rng.Next(1, 90)),
                Source = "manual",
                Status = "active"
            });
        }

        db.Patients.AddRange(patients);
        await db.SaveChangesAsync();
        _logger.LogInformation("Создано {Count} пациентов", patients.Count);
    }
}
