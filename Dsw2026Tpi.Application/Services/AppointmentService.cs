using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Dsw2026Tpi.CrossCutting.Resources;

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
        ValidateCreateRequest(request);

        var doctor = await _persistence.First<Doctor>(
            doctor =>
                doctor.Id == request.DoctorId &&
                !doctor.Deleted);

        if (doctor is null)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var patient = await _persistence.First<Patient>(
            patient =>
                patient.Dni == request.Patient.Dni &&
                !patient.Deleted);

        if (patient is null)
        {
            throw new EntityNotFoundException("Patient");
        }

        var slot = await _persistence.First<AvailabilitySlot>(
            availability =>
                availability.Id == request.AvailabilityId &&
                !availability.Deleted);

        if (slot is null)
        {
            throw new EntityNotFoundException("AvailabilitySlot");
        }

        if (slot.DoctorId != request.DoctorId)
        {
            throw new BusinessRuleException(
                "La disponibilidad no pertenece al médico seleccionado.",
                "INVALID_DOCTOR_SLOT");
        }

        ValidateSlotDate(slot);

        if (slot.Status != AvailabilityStatuses.Available)
        {
            throw new BusinessRuleException(
                "La disponibilidad seleccionada no está disponible.",
                "APPOINTMENT_CONFLICT");
        }

        var existingAppointment =
            await _persistence.First<Appointment>(
                appointment =>
                    appointment.AvailabilityId == request.AvailabilityId &&
                    appointment.Status == AppointmentStatuses.Booked);

        if (existingAppointment is not null)
        {
            throw new BusinessRuleException(
                "La disponibilidad seleccionada ya posee un turno.",
                "APPOINTMENT_CONFLICT");
        }

        var appointment = new Appointment(
            request.DoctorId,
            request.AvailabilityId,
            patient.Id,
            request.Reason.Trim());

        try
        {
            await _persistence.ExecuteInTransactionAsync(
                async () =>
                {
                    slot.MarkAsBooked();

                    await _persistence.Add(appointment);
                    await _persistence.Update(slot);
                    await _persistence.SaveChangesAsync();
                });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Conflicto de concurrencia al reservar la disponibilidad {AvailabilityId}.",
                request.AvailabilityId);

            throw new BusinessRuleException(
                "El turno fue reservado por otro paciente.",
                "APPOINTMENT_CONFLICT");
        }

        _logger.LogInformation(
            "Se reservó el turno {AppointmentId} para el médico {DoctorId} y el paciente {PatientId}.",
            appointment.Id,
            appointment.DoctorId,
            appointment.PatientId);

        return MapResponse(appointment, slot);
    }

    public async Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetByPatient(long dni)
    {
        ValidateDni(dni);

        var patient = await _persistence.First<Patient>(
            patient =>
                patient.Dni == dni &&
                !patient.Deleted);

        if (patient is null)
        {
            throw new EntityNotFoundException("Patient");
        }

        var appointments =
            await _persistence.GetFiltered<Appointment>(
                appointment =>
                    appointment.PatientId == patient.Id &&
                    appointment.Status ==
                    AppointmentStatuses.Booked);

        if (appointments is null || !appointments.Any())
        {
            return Array.Empty<AppointmentModel.Response>();
        }

        var appointmentList = appointments.ToList();

        var availabilityIds = appointmentList
            .Select(appointment => appointment.AvailabilityId)
            .ToList();

        var slots =
            await _persistence.GetFiltered<AvailabilitySlot>(
                slot =>
                    availabilityIds.Contains(slot.Id) &&
                    !slot.Deleted);

        if (slots is null || !slots.Any())
        {
            return Array.Empty<AppointmentModel.Response>();
        }

        var slotDictionary = slots.ToDictionary(
            slot => slot.Id);

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        return appointmentList
            .Where(appointment =>
                slotDictionary.ContainsKey(
                    appointment.AvailabilityId))
            .Select(appointment => new
            {
                Appointment = appointment,
                Slot = slotDictionary[
                    appointment.AvailabilityId]
            })
            .Where(item =>
                item.Slot.SlotDate > today ||
                (item.Slot.SlotDate == today &&
                 item.Slot.StartTime >= now.TimeOfDay))
            .OrderBy(item => item.Slot.SlotDate)
            .ThenBy(item => item.Slot.StartTime)
            .Select(item => MapResponse(
                item.Appointment,
                item.Slot))
            .ToList();
    }

    public async Task Cancel(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException(
                "El identificador del turno es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "id",
                    "Debe indicar un identificador válido.");
        }

        var appointment =
            await _persistence.GetById<Appointment>(id);

        if (appointment is null)
        {
            throw new EntityNotFoundException("Appointment");
        }

        var slot =
            await _persistence.GetById<AvailabilitySlot>(
                appointment.AvailabilityId);

        if (slot is null || slot.Deleted)
        {
            throw new EntityNotFoundException(
                "AvailabilitySlot");
        }

        ValidateCancellationDate(slot);

        try
        {
            await _persistence.ExecuteInTransactionAsync(
                async () =>
                {
                    appointment.Cancel();
                    slot.MarkAsAvailable();

                    await _persistence.Update(appointment);
                    await _persistence.Update(slot);
                    await _persistence.SaveChangesAsync();
                });
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Conflicto de concurrencia al cancelar el turno {AppointmentId}.",
                id);

            throw new BusinessRuleException(
                "El turno fue modificado por otra operación.",
                "APPOINTMENT_CONFLICT");
        }

        _logger.LogInformation(
            "Se canceló correctamente el turno {AppointmentId}.",
            appointment.Id);
    }

    public async Task<Pagination<AppointmentModel.Response>>
        GetByDate(
            DateOnly date,
            int pageSize,
            int pageIndex)
    {
        ValidatePagination(pageSize, pageIndex);

        var slots =
            await _persistence.GetFiltered<AvailabilitySlot>(
                slot =>
                    slot.SlotDate == date &&
                    !slot.Deleted);

        if (slots is null || !slots.Any())
        {
            return new Pagination<AppointmentModel.Response>(
                pageSize,
                pageIndex,
                0,
                Array.Empty<AppointmentModel.Response>());
        }

        var slotDictionary = slots.ToDictionary(
            slot => slot.Id);

        var availabilityIds = slotDictionary.Keys.ToList();

        var appointments =
            await _persistence.Paginate<Appointment, Guid>(
                pageSize,
                pageIndex,
                appointment =>
                    availabilityIds.Contains(
                        appointment.AvailabilityId),
                appointment => appointment.Id);

        return appointments.Map(
            appointment =>
            {
                var slot =
                    slotDictionary[
                        appointment.AvailabilityId];

                return MapResponse(
                    appointment,
                    slot);
            });
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

        if (specialtyId.HasValue &&
            specialtyId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "La especialidad indicada no es válida.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "specialtyId",
                    "Debe indicar una especialidad válida.");
        }

        if (doctorId.HasValue &&
            doctorId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "El médico indicado no es válido.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "doctorId",
                    "Debe indicar un médico válido.");
        }

        if (dni.HasValue)
        {
            ValidateDni(dni.Value);
        }

        if (specialtyId.HasValue)
        {
            var speciality =
                await _persistence.GetById<Speciality>(
                    specialtyId.Value);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException(
                    "Speciality");
            }
        }

        if (doctorId.HasValue)
        {
            var selectedDoctor =
                await _persistence.GetById<Doctor>(
                    doctorId.Value);

            if (selectedDoctor is null ||
                selectedDoctor.Deleted)
            {
                throw new EntityNotFoundException(
                    "Doctor");
            }
        }

        var doctors =
            await _persistence.GetFiltered<Doctor>(
                doctor =>
                    !doctor.Deleted &&
                    (!specialtyId.HasValue ||
                     doctor.SpecialityId ==
                     specialtyId.Value) &&
                    (!doctorId.HasValue ||
                     doctor.Id == doctorId.Value));

        if (doctors is null || !doctors.Any())
        {
            return EmptyPagination(
                pageSize,
                pageIndex);
        }

        var doctorIds = doctors
            .Select(doctor => doctor.Id)
            .ToList();

        IEnumerable<Guid>? patientIds = null;

        if (dni.HasValue)
        {
            var patient = await _persistence.First<Patient>(
                patient =>
                    patient.Dni == dni.Value &&
                    !patient.Deleted);

            if (patient is null)
            {
                return EmptyPagination(
                    pageSize,
                    pageIndex);
            }

            patientIds = new[] { patient.Id };
        }

        var slots =
            await _persistence.GetFiltered<AvailabilitySlot>(
                slot =>
                    !slot.Deleted &&
                    doctorIds.Contains(slot.DoctorId) &&
                    (!date.HasValue ||
                     slot.SlotDate == date.Value));

        if (slots is null || !slots.Any())
        {
            return EmptyPagination(
                pageSize,
                pageIndex);
        }

        var slotDictionary = slots.ToDictionary(
            slot => slot.Id);

        var availabilityIds = slotDictionary.Keys.ToList();

        var patientIdList = patientIds?.ToList();

        var appointments =
            await _persistence.Paginate<Appointment, Guid>(
                pageSize,
                pageIndex,
                appointment =>
                    doctorIds.Contains(
                        appointment.DoctorId) &&
                    availabilityIds.Contains(
                        appointment.AvailabilityId) &&
                    (patientIdList == null ||
                     patientIdList.Contains(
                         appointment.PatientId)),
                appointment => appointment.Id);

        return appointments.Map(
            appointment =>
            {
                var slot =
                    slotDictionary[
                        appointment.AvailabilityId];

                return MapResponse(
                    appointment,
                    slot);
            });
    }

    private static AppointmentModel.Response MapResponse(
        Appointment appointment,
        AvailabilitySlot slot)
    {
        return new AppointmentModel.Response(
            appointment.Id,
            appointment.Status,
            slot.SlotDate,
            slot.StartTime);
    }

    private static Pagination<AppointmentModel.Response>
        EmptyPagination(
            int pageSize,
            int pageIndex)
    {
        return new Pagination<AppointmentModel.Response>(
            pageSize,
            pageIndex,
            0,
            Array.Empty<AppointmentModel.Response>());
    }

    private static void ValidateCreateRequest(
        AppointmentModel.Request request)
    {
        if (request is null)
        {
            throw new ValidationException(
                "La solicitud es obligatoria.",
                ErrorCodes.VALIDATION_ERROR);
        }

        if (request.DoctorId == Guid.Empty)
        {
            throw new ValidationException(
                "DoctorId es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "doctorId",
                    "Debe indicar un médico válido.");
        }

        if (request.AvailabilityId == Guid.Empty)
        {
            throw new ValidationException(
                "AvailabilityId es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "availabilityId",
                    "Debe indicar una disponibilidad válida.");
        }

        if (request.Patient is null)
        {
            throw new ValidationException(
                "Patient es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "patient",
                    "Debe indicar los datos del paciente.");
        }

        ValidateDni(request.Patient.Dni);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException(
                "Reason es obligatorio.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "reason",
                    "El motivo es obligatorio.");
        }

        var normalizedReason = request.Reason.Trim();

        if (normalizedReason.Length < 5)
        {
            throw new ValidationException(
                "Reason debe tener al menos 5 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "reason",
                    "Debe tener al menos 5 caracteres.");
        }

        if (normalizedReason.Length > 500)
        {
            throw new ValidationException(
                "Reason no puede superar los 500 caracteres.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "reason",
                    "No puede superar los 500 caracteres.");
        }
    }

    private static void ValidateDni(long dni)
    {
        var dniLength = dni.ToString().Length;

        if (dni <= 0 ||
            dniLength < 7 ||
            dniLength > 10)
        {
            throw new ValidationException(
                "El DNI debe tener entre 7 y 10 dígitos.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "dni",
                    "Debe contener entre 7 y 10 dígitos.");
        }
    }

    private static void ValidateSlotDate(
        AvailabilitySlot slot)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var isPast =
            slot.SlotDate < today ||
            (slot.SlotDate == today &&
             slot.StartTime <= now.TimeOfDay);

        if (isPast)
        {
            throw new BusinessRuleException(
                "No se puede reservar un turno en el pasado.",
                "INVALID_APPOINTMENT_DATE");
        }
    }

    private static void ValidateCancellationDate(
        AvailabilitySlot slot)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var hasStarted =
            slot.SlotDate < today ||
            (slot.SlotDate == today &&
             slot.StartTime <= now.TimeOfDay);

        if (hasStarted)
        {
            throw new BusinessRuleException(
                "No se puede cancelar un turno que ya comenzó.",
                "INVALID_APPOINTMENT_DATE");
        }
    }

    private static void ValidatePagination(
        int pageSize,
        int pageIndex)
    {
        if (pageSize <= 0)
        {
            throw new ValidationException(
                "El tamaño de página debe ser mayor que cero.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "pageSize",
                    "Debe ser mayor que cero.");
        }

        if (pageIndex < 0)
        {
            throw new ValidationException(
                "El índice de página no puede ser negativo.",
                ErrorCodes.VALIDATION_ERROR)
                .WithDetail(
                    "pageIndex",
                    "No puede ser negativo.");
        }
    }

}