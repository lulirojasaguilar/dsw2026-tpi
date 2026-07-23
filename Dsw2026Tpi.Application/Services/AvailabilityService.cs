using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly Dsw2026TpiDbContext _context;
        private readonly ILogger<AvailabilityService> _logger;

        public AvailabilityService(Dsw2026TpiDbContext context, ILogger<AvailabilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AvailabilityRule> CreateAvailabilityRuleAsync(
            Guid doctorId,
            int month,
            int year,
            int dayofWeek,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            _logger.LogInformation("Iniciando generación de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}", doctorId, month, year);

            if (startTime >= endTime)
            {
                throw new ValidationException("VALIDATION_ERROR", "StartTime debe ser estrictamente menor a EndTime.");
            }

            var doctorExists = await _context.Set<Doctor>().AnyAsync(d => d.Id == doctorId && d.IsActive);
            if (!doctorExists)
            {
                throw new EntityNotFoundException("Doctor");
            }

            var existingRule = _context.Set<AvailabilityRule>()
                .Where(r => r.DoctorId == doctorId
                && r.Month == month
                && r.Year == year
                && r.DayOfWeek == dayofWeek
                && !r.Deleted
                );

            foreach (var existing in existingRule)
            {
                if (startTime < existing.EndTime && existing.StartTime < endTime)
                {
                    _logger.LogWarning("Conflicto de solapamiento detectado para el doctor {DoctorId} en el dia de la semana{DayOfWeek}", doctorId, dayofWeek);
                    throw new BusinessRuleException("SCHEDULE_OVERLAP"," Ya existe una regla de disponibilidad que se solapa con el rango horario indicado para este dia");

                }
            }
                var rule = new AvailabilityRule(doctorId, month, year, dayofWeek, startTime, endTime);

                _context.Set<AvailabilityRule>().Add(rule);
                var slots = GenerateSlotsForMonth(rule.Id, month, year, dayofWeek, startTime, endTime);
                await _context.Set<AvailabilitySlot>().AddRangeAsync(slots);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Se generaron exitosamente los slots de disponibilidades para la regla {RuleID}", rule.Id);
                return rule;
            
        }


        public async Task<AvailabilityRule> UpdateAvailabilityRuleAsync(
                   Guid doctorId,
                   int month,
                   int year,
                   int dayOfWeek,
                   TimeSpan startTime,
                   TimeSpan endTime)
        {
            _logger.LogInformation("Iniciando actualización de disponibilidad para el doctor {DoctorId} en el periodo {Month}/{Year}, dia {DayOfWeek}", doctorId, month, year, dayOfWeek);

            if (startTime >= endTime)
            {
                throw new ValidationException("VALIDATION_ERROR", "StartTime debe ser estrictamente menor a EndTime.");
            }

            var doctorExists = await _context.Set<Doctor>().AnyAsync(d => d.Id == doctorId && d.IsActive);
            if (!doctorExists)
            {
                throw new EntityNotFoundException("Doctor");
            }

           
            var existingRule = await _context.Set<AvailabilityRule>()
                .FirstOrDefaultAsync(r => r.DoctorId == doctorId
                                       && r.Month == month
                                       && r.Year == year
                                       && r.DayOfWeek == dayOfWeek
                                       && !r.Deleted);

            if (existingRule == null)
            {
                throw new EntityNotFoundException("AvailabilityRule");
            }

        
            var existingSlots = await _context.Set<AvailabilitySlot>()
                .Where(s => s.AvailabilityRuleId == existingRule.Id && !s.Deleted)
                .ToListAsync();

          
            if (existingSlots.Any(s => s.Status == "BOOKED"))
            {
                _logger.LogWarning("Intento de sobrescritura fallido: El doctor {DoctorId} ya tiene turnos reservados para este dia.", doctorId);
                throw new BusinessRuleException("UPDATE_CONFLICT", "No se puede sobrescribir el mes porque ya existen turnos reservados (BOOKED) para este día.");
            }

           
            existingRule.Delete();
            foreach (var slot in existingSlots)
            {
                slot.Delete();
            }

          
            var newRule = new AvailabilityRule(doctorId, month, year, dayOfWeek, startTime, endTime);
            _context.Set<AvailabilityRule>().Add(newRule);

            var newSlots = GenerateSlotsForMonth(newRule.Id, month, year, dayOfWeek, startTime, endTime);
            await _context.Set<AvailabilitySlot>().AddRangeAsync(newSlots);

           
            await _context.SaveChangesAsync();

            _logger.LogInformation("Se sobrescribió exitosamente la disponibilidad para la nueva regla {RuleID}", newRule.Id);
            return newRule;
        }



        private List<AvailabilitySlot> GenerateSlotsForMonth(Guid ruleId, int month, int year, int targetDayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            var slots = new List<AvailabilitySlot>();

            int daysInMonth = DateTime.DaysInMonth(year, month);

            var holidays = GetHolidaysForYear(year);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateOnly(year, month, day);

                if ((int)currentDate.DayOfWeek != targetDayOfWeek)
                {
                    continue;
                }

                if (holidays.Contains(currentDate))
                {
                    _logger.LogWarning("La fecha {Date} es un dia no laborable/feriado. Se omite la generacion de turnos.", currentDate);
                    continue;
                }
                var currentSlotStart = startTime;
                while (currentSlotStart.Add(TimeSpan.FromMinutes(30)) <= endTime)
                {
                    var currentSlotEnd = currentSlotStart.Add(TimeSpan.FromMinutes(30));

                    var slot = new AvailabilitySlot(
                        ruleId,
                        currentDate,
                        currentSlotStart,
                        currentSlotEnd,
                        status: "AVAILABLE"
                        );
                    slots.Add(slot);
                    currentSlotStart = currentSlotEnd;
                }
            }
            return slots;
        }




        private HashSet<DateOnly> GetHolidaysForYear(int year)
        {
            return new HashSet<DateOnly>
            {
                new DateOnly(year, 7, 9),
                new DateOnly(year, 12, 25),
            };
        }

    }
}