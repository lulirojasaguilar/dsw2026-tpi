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

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null, Guid? specialtyId = null)
    {
        if (pageSize <= 0)
        {
            throw new ValidationException(
                "El tamaño de página debe ser mayor que cero.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("pageSize", "Debe ser mayor que cero.");
        }

        if (pageIndex < 0)
        {
            throw new ValidationException(
                "El índice de página no puede ser negativo.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("pageIndex", "No puede ser negativo.");
        }

        var normalizedName = name?.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedName) &&
           (normalizedName.Length < 3 || normalizedName.Length > 100))
        {
            throw new ValidationException(
                "El filtro por nombre debe tener entre 3 y 100 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }

        if (specialtyId.HasValue)
        {
            var speciality =
                await _persistence.GetById<Speciality>(
                    specialtyId.Value);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }
        }

        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, pageIndex,
            d => !d.Deleted && (string.IsNullOrWhiteSpace(normalizedName) || d.Name.Contains(normalizedName)) && 
            (!specialtyId.HasValue ||
            d.SpecialityId == specialtyId.Value),
            d => d.Name,
            nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber, new DoctorModel.SpecialityDto(d.Speciality.Id, d.Speciality.Name)));
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        ValidateRequest(request);

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        var normalizedName = request.Name.Trim();

        var normalizedLicenseNumber = request.LicenseNumber.Trim();

        var doctor = new Doctor(
            normalizedName,
            normalizedLicenseNumber,
            speciality);

        var createdDoctor = await _persistence.Add(doctor);

        await _persistence.SaveChangesAsync();

        return new DoctorModel.Response(createdDoctor.Id, createdDoctor.Name, createdDoctor.LicenseNumber, new DoctorModel.SpecialityDto( speciality.Id, speciality.Name));
    }
    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.GetById<Doctor>(id, nameof(Doctor.Speciality));

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        var normalizedName = request.Name.Trim();

        var normalizedLicenseNumber = request.LicenseNumber.Trim();

        doctor.Update(
            normalizedName,
            normalizedLicenseNumber,
            speciality);

        var updatedDoctor = await _persistence.Update(doctor);

        await _persistence.SaveChangesAsync();

        return new DoctorModel.Response(updatedDoctor.Id, updatedDoctor.Name, updatedDoctor.LicenseNumber, new DoctorModel.SpecialityDto(speciality.Id,  speciality.Name));
    }

    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        doctor.Delete();

        await _persistence.Update(doctor);

        await _persistence.SaveChangesAsync();
    }

    public async Task<IReadOnlyCollection<DoctorModel.AvailabilityResponse>>
        GetAvailabilities(Guid doctorId)
    {
        var doctor = await _persistence.GetById<Doctor>(doctorId);

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var currentDate = DateOnly.FromDateTime(DateTime.Today);

        var rules = await _persistence.GetFiltered<AvailabilityRule>(
            rule =>
                rule.DoctorId == doctorId &&
                rule.Month == currentDate.Month &&
                rule.Year == currentDate.Year &&
                !rule.Deleted);

        if (rules is null || !rules.Any())
        {
            return Array.Empty<DoctorModel.AvailabilityResponse>();
        }

        return rules
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .Select(rule => new DoctorModel.AvailabilityResponse(
                GetDayName(rule.DayOfWeek),
                rule.StartTime.ToString(@"hh\:mm"),
                rule.EndTime.ToString(@"hh\:mm")))
            .ToList();
    }

    private static void ValidateRequest(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "El nombre es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "El nombre es obligatorio.");
        }

        var normalizedName = request.Name.Trim();

        if (normalizedName.Length < 3 || normalizedName.Length > 100)
        {
            throw new ValidationException(
                "El nombre debe tener entre 3 y 100 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            throw new ValidationException(
                "La matrícula es obligatoria.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "licenseNumber",
                    "La matrícula es obligatoria.");
        }

        var normalizedLicenseNumber =
            request.LicenseNumber.Trim();

        if (normalizedLicenseNumber.Length > 50)
        {
            throw new ValidationException(
                "La matrícula no puede superar los 50 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "licenseNumber",
                    "No puede superar los 50 caracteres.");
        }

        if (request.SpecialityId == Guid.Empty)
        {
            throw new ValidationException(
                "La especialidad es obligatoria.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "specialityId",
                    "Debe indicar una especialidad válida.");
        }
    }

    private static string GetDayName(byte dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => "MONDAY",
            1 => "TUESDAY",
            2 => "WEDNESDAY",
            3 => "THURSDAY",
            4 => "FRIDAY",
            5 => "SATURDAY",
            6 => "SUNDAY",
            _ => throw new ArgumentOutOfRangeException(
                nameof(dayOfWeek),
                dayOfWeek,
                "Día no válido.")
        };
    }

}
