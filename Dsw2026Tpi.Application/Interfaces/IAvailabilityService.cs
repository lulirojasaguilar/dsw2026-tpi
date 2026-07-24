using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task<AvailabilityModel.Response> CreateAvailabilityAsync(AvailabilityModel.Request request);
        Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request);
    }
}
