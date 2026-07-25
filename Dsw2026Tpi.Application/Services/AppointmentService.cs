using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Constants;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;

    public AppointmentService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<AppointmentModel.Response> Create(
        AppointmentModel.Request request)
    {
        var doctor = await _persistence.First<Doctor>(
            d => d.Id == request.DoctorId);

        if (doctor is null)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var patient = await _persistence.First<Patient>(
            p => p.Dni == request.Patient.Dni && !p.Deleted);

        if (patient is null)
        {
            throw new EntityNotFoundException("Patient");
        }

        var slot = await _persistence.First<AvailabilitySlot>(
            a => a.Id == request.AvailabilityId && !a.Deleted);

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

        var today = DateOnly.FromDateTime(DateTime.Now);
        var currentTime = DateTime.Now.TimeOfDay;

        var isPast =
            slot.SlotDate < today ||
            (slot.SlotDate == today && slot.StartTime <= currentTime);

        if (isPast)
        {
            throw new BusinessRuleException(
                "No se puede reservar un turno en el pasado.",
                "INVALID_APPOINTMENT_DATE");
        }

        if (slot.Status != AvailabilityStatuses.Available)
        {
            throw new BusinessRuleException(
                "La disponibilidad seleccionada no está disponible.",
                "APPOINTMENT_CONFLICT");
        }

        var appointment = new Appointment(
            request.DoctorId,
            request.AvailabilityId,
            patient.Id,
            request.Reason);

        await _persistence.ExecuteInTransactionAsync(async () =>
        {
            slot.MarkAsBooked();

            await _persistence.Add(appointment);
            await _persistence.Update(slot);

            await _persistence.SaveChangesAsync();
        });

        return new AppointmentModel.Response(
            appointment.Id,
            appointment.Status,
            slot.SlotDate,
            slot.StartTime);
    }
}