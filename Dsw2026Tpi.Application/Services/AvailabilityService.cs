using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Entities;
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

            var rule = new AvailabilityRule(doctorId, month, year, dayofWeek, startTime, endTime);

            _context.Set<AvailabilityRule>().Add(rule);
            var slots = GenerateSlotsForMonth(rule.Id, month, year, dayofWeek, startTime, endTime);
            await _context.Set<AvailabilitySlot>().AddRangeAsync(slots);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Se generaron exitosamente los slots de disponibilidades para la regla {RuleID}", rule.Id);
            return rule;
        }

        private List<AvailabilitySlot> GenerateSlotsForMonth(Guid ruleId, int month, int year, int targetDayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            var slots = new List<AvailabilitySlot>();

            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++) {
                var currentDate = new DateOnly(year, month, day);

                if ((int)currentDate.DayOfWeek == targetDayOfWeek) {
                    if ((IsHolidays(currentDate))){
                        _logger.LogWarning("La fecha {Date} es feriado. No se generaran turnos", currentDate);
                        continue;
                    }
                    var currentSlotStart = startTime;
                    while (currentSlotStart.Add(TimeSpan.FromMinutes(30)) <= endTime)
                    {
                        var currentSlotEnd = currentSlotStart.Add(TimeSpan.FromMinutes(30));
                        var slot = new AvailabilitySlot(ruleId, currentDate, currentSlotStart, currentSlotEnd, status: "AVAILABLE");
                        slots.Add(slot);
                        currentSlotStart = currentSlotEnd;
                    }
                }
            }
            return slots;
        }
        private bool IsHolidays(DateOnly date)
        {
            return false;
        }
    }
    }