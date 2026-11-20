using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    private readonly List<AvailabilityRule> _availabilities = [];

    public string Name { get; private set; }

    public string? LicenseNumber { get; private set; }

    public bool Deleted { get; private set; }

    public bool IsBookable => !Deleted;

    public Guid SpecialityId { get; private set; }

    public Speciality Speciality { get; private set; }

    public IReadOnlyCollection<AvailabilityRule> Availabilities => _availabilities.AsReadOnly();

    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor()
    {
    }
#pragma warning restore CS8618
    #endregion

    internal Doctor(
        string name,
        string? licenseNumber,
        Speciality speciality,
        Guid? id = null) : base(id)
    {
        Validate(name, licenseNumber, speciality);

        Name = name.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();
        Deleted = false;
        AssignSpeciality(speciality);
    }

    public void Update(
        string name,
        string? licenseNumber,
        Speciality speciality)
    {
        Validate(name, licenseNumber, speciality);

        Name = name.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();

        if (speciality.Id != SpecialityId)
        {
            Speciality?.RemoveDoctor(this);
            AssignSpeciality(speciality);
        }
    }

    public void Delete()
    {
        Deleted = true;
    }


    public bool HasAvailabilityOverlap(
        byte month,
        short year,
        byte dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime) =>
        _availabilities.Any(rule =>
            !rule.Deleted
            && rule.Month == month
            && rule.Year == year
            && rule.DayOfWeek == dayOfWeek
            && startTime < rule.EndTime
            && rule.StartTime < endTime);


    public AvailabilityRule AddAvailability(
        byte month,
        short year,
        byte dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime)
    {
        if (HasAvailabilityOverlap(month, year, dayOfWeek, startTime, endTime))
        {
            throw new ConflictException(
                ErrorCodes.SCHEDULE_OVERLAP,
                nameof(ErrorCodes.SCHEDULE_OVERLAP));
        }

        var rule = new AvailabilityRule(this, month, year, dayOfWeek, startTime, endTime);

        _availabilities.Add(rule);

        return rule;
    }

    public void ClearAvailabilities(
        byte month,
        short year,
        DateOnly today,
        TimeSpan nowTime,
        DateTime now)
    {
        foreach (var rule in _availabilities.Where(r => !r.Deleted && r.Month == month && r.Year == year))
        {
            rule.DeleteFreeFutureSlots(today, nowTime, now);
            rule.Delete();
            rule.UpdatedAt = now;
        }
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Speciality))]
    private void AssignSpeciality(Speciality speciality)
    {
        Speciality = speciality;
        SpecialityId = speciality.Id;
        speciality.Register(this);
    }

    private static void Validate(
        string name,
        string? licenseNumber,
        Speciality speciality)
    {
        var normalizedName = name?.Trim() ?? string.Empty;

        if (normalizedName.Length is < 3 or > 100)
        {
            throw new ValidationException(
                "El nombre del médico no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "La longitud debe estar entre 3 y 100 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(licenseNumber) && licenseNumber.Trim().Length > 50)
        {
            throw new ValidationException(
                "El número de matrícula no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("licenseNumber", "No puede superar los 50 caracteres.");
        }

        if (speciality is null || speciality.Id == Guid.Empty)
        {
            throw new ValidationException(
                "La especialidad indicada no es válida.",
                nameof(ErrorCodes.SPECIALITY_NOT_FOUND))
                .WithDetail("specialityId", "Debe corresponder a una especialidad existente.");
        }
    }
}
