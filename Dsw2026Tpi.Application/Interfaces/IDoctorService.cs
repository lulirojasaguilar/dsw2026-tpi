using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<Pagination<DoctorModel.Response>> GetAll( 
        int pageSize, 
        int pageIndex, 
        string? name = null, 
        Guid? specialtyId = null);
    Task<DoctorModel.Response> Create(
        DoctorModel.Request request);
    Task<DoctorModel.Response> Update(
        Guid id, 
        DoctorModel.Request request);
    Task Delete(
        Guid id);
    Task<IReadOnlyCollection<DoctorModel.AvailabilityResponse>>  GetAvailabilities(
        Guid doctorId,
        byte? month = null,
        short? year = null);
}
