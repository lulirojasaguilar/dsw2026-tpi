using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private const int MaxSlotRangeDays = 31;

    private readonly IPersistence _persistence;
    private readonly IClinicClock _clock;

    public DoctorService(IPersistence persistence, IClinicClock clock)
    {
        _persistence = persistence;
        _clock = clock;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
    {
        RequestValidations.ValidatePagination(pageSize, pageIndex);

        var normalizedName = RequestValidations.NormalizeNameFilter(name);

        var page = await _persistence.Paginate<Doctor, string>(
            pageSize,
            pageIndex,
            d => !d.Deleted
                && (normalizedName == null || d.Name.Contains(normalizedName)),
            d => d.Name,
            nameof(Doctor.Speciality));

        return page.Map(Map);
    }

    public async Task<DoctorModel.Response> GetById(
        Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id, nameof(Doctor.Speciality));

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        return Map(doctor);
    }

    public async Task<DoctorModel.Response> Create(
        DoctorModel.Request request)
    {
        var speciality = await _persistence.GetById<Speciality>(request.SpecialtyId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        var doctor = speciality.AddDoctor(request.Name, request.LicenseNumber);

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

        var today = _clock.Today;

        var hasBookedAppointments = await _persistence.Any<Appointment>(
            appointment => appointment.DoctorId == id
                && appointment.Status == AppointmentStatuses.Booked
                && appointment.AvailabilitySlot.SlotDate >= today);

        if (hasBookedAppointments)
        {
            throw new ConflictException(
                ErrorCodes.DOCTOR_HAS_APPOINTMENTS,
                nameof(ErrorCodes.DOCTOR_HAS_APPOINTMENTS));
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

        var today = _clock.Today;

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

    public async Task<IReadOnlyCollection<DoctorModel.SlotResponse>> GetSlots(
        Guid doctorId,
        DateOnly from,
        DateOnly to)
    {
        if (from > to)
        {
            throw new ValidationException(
                "El rango de fechas no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("from", "from no puede ser posterior a to.");
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxSlotRangeDays)
        {
            throw new ValidationException(
                "El rango de fechas no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("to", $"El rango no puede superar los {MaxSlotRangeDays} días.");
        }

        var doctor = await _persistence.GetById<Doctor>(doctorId);

        if (doctor is null || !doctor.IsBookable)
        {
            throw new EntityNotFoundException(ErrorCodes.DOCTOR_NOT_FOUND, nameof(ErrorCodes.DOCTOR_NOT_FOUND));
        }

        var slots = await _persistence.GetFiltered<AvailabilitySlot>(
            slot => slot.DoctorId == doctorId
                && !slot.Deleted
                && slot.Status == AvailabilityStatuses.Available
                && slot.SlotDate >= from
                && slot.SlotDate <= to);

        var now = _clock.Now;

        return (slots ?? [])
            .Where(slot => !slot.HasStarted(now))
            .OrderBy(slot => slot.SlotDate)
            .ThenBy(slot => slot.StartTime)
            .Select(slot => new DoctorModel.SlotResponse(
                slot.Id,
                slot.SlotDate,
                slot.StartTime.ToString(@"hh\:mm")))
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
