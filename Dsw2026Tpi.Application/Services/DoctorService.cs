using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;


namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(
        int pageSize, 
        int pageIndex, 
        string? name = null)
    {
        if (pageSize <= 0)
        {
            throw new ValidationException(
                "El tamaño de página debe ser mayor que cero.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("pageSize", "Debe ser mayor que cero.");
        }

        if (pageIndex < 0)
        {
            throw new ValidationException(
                "El índice de página no puede ser negativo.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("pageIndex", "No puede ser negativo.");
        }

        if (!string.IsNullOrWhiteSpace(name) &&
            (name.Length < 3 || name.Length > 100))
        {
            throw new ValidationException(
                "El filtro por nombre debe tener entre 3 y 100 caracteres.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }

        var normalizedName = name?.Trim();

        var page = await _persistence.Paginate<Doctor, string>(
            pageSize, 
            pageIndex,
            d => !d.Deleted 
            && (string.IsNullOrWhiteSpace(normalizedName) || 
            d.Name.Contains(normalizedName)),
            d => d.Name,
            nameof(Doctor.Speciality));

        return page.Map(Map);
    }

    public async Task<DoctorModel.Response> Create(
        DoctorModel.Request request)
    {
        var speciality = await _persistence.GetById<Speciality>(request.SpecialtyId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);

        var now = DateTime.UtcNow;
        doctor.CreatedAt = now;
        doctor.UpdatedAt = now;

        await _persistence.Add(doctor);
        await _persistence.SaveChangesAsync();

        return Map(doctor);
    }
    public async Task<DoctorModel.Response> Update(
        Guid id, 
        DoctorModel.Request request)
    {
        var doctor = await _persistence.GetById<Doctor>(id, nameof(Doctor.Speciality))
            ?? throw new EntityNotFoundException("Doctor");

        if (doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var speciality = await _persistence.GetById<Speciality>(request.SpecialtyId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        doctor.Update(request.Name, request.LicenseNumber, speciality);
        doctor.UpdatedAt = DateTime.UtcNow;

        await _persistence.Update(doctor);
        await _persistence.SaveChangesAsync();

        return Map(doctor);
    }

    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
          ?? throw new EntityNotFoundException("Doctor");

        if (doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        doctor.Delete();
        doctor.UpdatedAt = DateTime.UtcNow;

        await _persistence.Update(doctor);
        await _persistence.SaveChangesAsync();

    }

    public async Task<IReadOnlyCollection<DoctorModel.AvailabilityResponse>>
        GetAvailabilities(
        Guid doctorId)
    {
        await EnsureDoctorExists(doctorId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var rules = await _persistence.GetFiltered<AvailabilityRule>(
            rule => rule.DoctorId == doctorId
                && !rule.Deleted
                && rule.Month == today.Month
                && rule.Year == today.Year);

        return (rules ?? [])
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .Select(rule => new DoctorModel.AvailabilityResponse(
                rule.Id,
                AvailabilityGenerator.DayName(rule.DayOfWeek),
                rule.StartTime.ToString(@"hh\:mm"),
                rule.EndTime.ToString(@"hh\:mm")))
            .ToList();
    }

    private static DoctorModel.Response Map(Doctor doctor) => new(
       doctor.Id,
       doctor.Name,
       doctor.LicenseNumber,
       new DoctorModel.SpecialityDto(doctor.Speciality.Id, doctor.Speciality.Name));

    private async Task EnsureDoctorExists(Guid doctorId)
    {
        var doctor = await _persistence.GetById<Doctor>(doctorId);

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }
    }
}

