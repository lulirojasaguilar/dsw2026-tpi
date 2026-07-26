using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentModel.Response> Create(
            AppointmentModel.Request request);

        Task<IReadOnlyCollection<AppointmentModel.Response>> GetByPatient(
            Guid patientId);

        Task Cancel(
            Guid id,
            Guid patientId);

        Task<Pagination<AppointmentModel.Response>> GetByDate(
            DateOnly date,
            int pageSize,
            int pageIndex);

        Task<Pagination<AppointmentModel.Response>> Search(
            int pageSize,
            int pageIndex,
            Guid? specialtyId = null,
            Guid? doctorId = null,
            long? dni = null,
            DateOnly? date = null);
    }
}
