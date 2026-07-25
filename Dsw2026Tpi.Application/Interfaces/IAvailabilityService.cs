using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task<AvailabilityModel.Response> CreateAvailabilityAsync(AvailabilityModel.Request request);
        Task<AvailabilityModel.Response> UpdateAvailabilityAsync(AvailabilityModel.Request request);
    }
}
