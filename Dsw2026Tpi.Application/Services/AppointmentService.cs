using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
           IPersistence persistence,
           ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _logger = logger;
    }

    public async Task<AppointmentModel.Response> Create(
        AppointmentModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.First<Doctor>(
            doctor =>
                doctor.Id == request.DoctorId &&
                !doctor.Deleted)
            ?? throw new EntityNotFoundException(ErrorCodes.DOCTOR_NOT_FOUND, nameof(ErrorCodes.DOCTOR_NOT_FOUND));

        var dni = request.Patient.Dni.ToString();

        var patient = await _persistence.First<Patient>(
            patient =>
                patient.Dni == dni &&
                !patient.Deleted)
            ?? throw new EntityNotFoundException(ErrorCodes.PATIENT_NOT_FOUND, nameof(ErrorCodes.PATIENT_NOT_FOUND));

        AppointmentModel.Response response = null!;

        await _persistence.ExecuteInTransactionAsync(async () =>
        {
            var slot = await _persistence.First<AvailabilitySlot>(
                s => s.Id == request.AvailabilitySlotId && 
                s.DoctorId == doctor.Id && 
                !s.Deleted,
                "AvailabilityRule.Doctor.Speciality")
                ?? throw new EntityNotFoundException(ErrorCodes.AVAILABILITY_NOT_FOUND, nameof(ErrorCodes.AVAILABILITY_NOT_FOUND));

            var now = DateTime.UtcNow;
            var slotDateTime = slot.SlotDate.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime));

            if (slotDateTime <= now)
            {
                _logger.LogWarning(
                    "Intento de reserva rechazado por fecha/hora pasada. DoctorId={DoctorId} AvailabilitySlotId={AvailabilitySlotId}",
                    request.DoctorId, request.AvailabilitySlotId);

                throw new ValidationException(
                    "No se pueden reservar turnos en fechas u horarios pasados.",
                    nameof(ErrorCodes.PAST_DATETIME_NOT_ALLOWED));
            }

            slot.MarkBooked();
            slot.UpdatedAt = now;
            await _persistence.Update(slot);

            var appointment = new Appointment(slot, patient, request.Reason);
            appointment.CreatedAt = now;
            appointment.UpdatedAt = now;

            await _persistence.Add(appointment);

            try
            {
                await _persistence.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    "Conflicto de reserva por índice único. AvailabilitySlotId={AvailabilitySlotId}",
                    request.AvailabilitySlotId);

                throw new ConflictException("No se pudo completar la reserva porque el turno fue reservado por otro paciente.",
                    nameof(ErrorCodes.APPOINTMENT_CONFLICT));
            }

            _logger.LogInformation(
                    "Turno reservado. AppointmentId={AppointmentId} DoctorId={DoctorId} PatientId={PatientId}",
                     appointment.Id, doctor.Id, patient.Id);

            response = MapResponse(appointment);
        });


        return response;
    }

    public async Task<IReadOnlyCollection<AppointmentModel.Response>> GetByPatient(
        long dni,
        Guid requestingPatientId)
    {
        var patient = await _persistence.First<Patient>(
            patient =>
                patient.Dni == dni.ToString() &&
                !patient.Deleted)
            ?? throw new EntityNotFoundException(ErrorCodes.PATIENT_NOT_FOUND, nameof(ErrorCodes.PATIENT_NOT_FOUND));

        if (patient.Id != requestingPatientId)
        {
            _logger.LogWarning(
                "Intento de acceso a turnos de un DNI que no pertenece al paciente autenticado. RequestingPatientId={RequestingPatientId}",
                requestingPatientId);

            throw new AuthorizationException(nameof(ErrorCodes.PATIENT_MISMATCH));
        }

        var appointments =
            await _persistence.GetFiltered<Appointment>(
                appointment =>
                    appointment.PatientId == patient.Id &&
                    appointment.Status ==
                    AppointmentStatuses.Booked,
                    "AvailabilitySlot", "Doctor.Speciality", "Patient");

        return (appointments ?? [])
            .OrderBy(a => a.AvailabilitySlot.SlotDate).ThenBy(a => a.AvailabilitySlot.StartTime)
            .Select(MapResponse)
            .ToList();
    }

    public async Task Cancel(
        Guid id, 
        Guid patientId)
    {
        var appointment = await _persistence.First<Appointment>(
                a => 
                    a.Id == id, 
                    "AvailabilitySlot")
                ?? throw new EntityNotFoundException(
                    ErrorCodes.APPOINTMENT_NOT_FOUND, 
                    nameof(ErrorCodes.APPOINTMENT_NOT_FOUND));

        if (appointment.PatientId != patientId)
        {
            _logger.LogWarning(
                "Intento de cancelar un turno que no pertenece al paciente autenticado. AppointmentId={AppointmentId} RequestingPatientId={RequestingPatientId}",
                id, 
                patientId);

            throw new AuthorizationException(
                ErrorCodes.PATIENT_MISMATCH);
        }

        appointment.Cancel();
        appointment.UpdatedAt = DateTime.UtcNow;

        appointment.AvailabilitySlot.MarkAvailable();
        appointment.AvailabilitySlot.UpdatedAt = DateTime.UtcNow;

        await _persistence.Update(appointment);
        await _persistence.Update(appointment.AvailabilitySlot);
        await _persistence.SaveChangesAsync();

        _logger.LogInformation(
            "Turno cancelado. AppointmentId={AppointmentId} PatientId={PatientId}", 
            id, 
            patientId);
    }

    public async Task<Pagination<AppointmentModel.Response>> GetByDate(
            DateOnly date,
            int pageSize,
            int pageIndex)
    {
        ValidatePagination(pageSize, pageIndex);

        var page = await _persistence.Paginate<Appointment, TimeSpan>(
            pageSize,
            pageIndex,
            a => a.AvailabilitySlot.SlotDate == date,
            a => a.AvailabilitySlot.StartTime,
            "AvailabilitySlot", "Doctor.Speciality", "Patient");

        return page.Map(MapResponse);
    }

    public async Task<Pagination<AppointmentModel.Response>>
        Search(
            int pageSize,
            int pageIndex,
            Guid? specialtyId = null,
            Guid? doctorId = null,
            long? dni = null,
            DateOnly? date = null)
    {
        ValidatePagination(pageSize, pageIndex);

        var dniText = dni?.ToString();

        var page = await _persistence.Paginate<Appointment, DateOnly>(
            pageSize,
            pageIndex,
            a => (doctorId == null || a.DoctorId == doctorId)
                && (specialtyId == null || a.Doctor.SpecialityId == specialtyId)
                && (dniText == null || a.Patient.Dni == dniText)
                && (date == null || a.AvailabilitySlot.SlotDate == date),
            a => a.AvailabilitySlot.SlotDate,
            "AvailabilitySlot", "Doctor.Speciality", "Patient");

        return page.Map(MapResponse);
    }

    private static void ValidateRequest(
        AppointmentModel.Request request)
    {
        if (request.DoctorId == Guid.Empty)
        {
            throw new ValidationException("El médico indicado no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("doctorId", "doctorId es obligatorio.");
        }

        if (request.AvailabilitySlotId == Guid.Empty)
        {
            throw new ValidationException("El turno indicado no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("availabilitySlotId", "availabilitySlotId es obligatorio.");
        }

        if (request.Patient is null || !request.Patient.Dni.DigitCountBetween(7, 10))
        {
            throw new ValidationException("El DNI del paciente no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("patient.dni", "El DNI es obligatorio y debe tener entre 7 y 10 dígitos.");
        }

        var normalizedReason = request.Reason?.Trim() ?? string.Empty;

        if (normalizedReason.Length is < 5 or > 300)
        {
            throw new ValidationException("El motivo de la consulta no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("reason", "El motivo es obligatorio y debe tener entre 5 y 300 caracteres.");
        }
    }

    private static void ValidatePagination(int pageSize, int pageIndex)
    {
        if (pageSize <= 0 || pageIndex < 0)
        {
            throw new ValidationException("Los parámetros de paginación no son válidos.", nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("pageSize", "pageSize debe ser mayor a 0 y pageIndex mayor o igual a 0.");
        }
    }

    private static AppointmentModel.Response MapResponse(Appointment appointment) => new(
        appointment.Id,
        appointment.Status,
        appointment.Reason,
        appointment.AvailabilitySlot.SlotDate,
        appointment.AvailabilitySlot.StartTime.ToString(@"hh\:mm"),
        appointment.AvailabilitySlot.EndTime.ToString(@"hh\:mm"),
        appointment.CancelledAt,
        appointment.AttendedAt,
        new AppointmentModel.DoctorDto(
            appointment.Doctor.Id,
            appointment.Doctor.Name,
            new AppointmentModel.SpecialityDto(appointment.Doctor.Speciality.Id, appointment.Doctor.Speciality.Name)),
        new AppointmentModel.PatientDto(
            appointment.Patient.Id,
            long.Parse(appointment.Patient.Dni),
            appointment.Patient.FullName ?? ""));
}