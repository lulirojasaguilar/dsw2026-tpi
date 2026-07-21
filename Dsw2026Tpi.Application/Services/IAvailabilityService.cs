using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public interface IAvailabilityService
    {
        Task<AvailabilityRule> CreateAvailabilityRuleAsync(
            Guid doctorId,
            int month,
            int year,
            int dayOfWeek,
            TimeSpan startTime,
            TimeSpan endTime);

    }
}
