using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System.Linq;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService
    {
        private readonly IPersistence _persistence;
        public SpecialityService(IPersistence persistence)
        {
            _persistence = persistence;
        }
        public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
        {
            var specialities = await _persistence.Paginate<Speciality, string>(pageSize, pageIndex, s =>!s.Deleted && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)), s => s.Name);
            return specialities.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            ValidateRequest(request);

            var existingSpecialities = await _persistence.GetFiltered<Speciality>(s => s.Name == request.Name && !s.Deleted);

            if (existingSpecialities?.Any() == null)
            {
                throw new ConflictException(ErrorCodes.VALIDATION_ERROR,"Ya existe una especialidad con ese nombre.");
            }

            var speciality = new Speciality(
                request.Name,
                request.Description);

            var createdSpeciality =
                await _persistence.Add(speciality);

            return new SpecialityModel.Response(
                createdSpeciality.Id,
                createdSpeciality.Name,
                createdSpeciality.Description);
        }

        public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
        {
            ValidateRequest(request);

            var speciality = await _persistence.GetById<Speciality>(id);

            if (speciality is null)
            {
                throw new EntityNotFoundException("Speciality");
            }

            var existingSpecialities = await _persistence.GetFiltered<Speciality>(s => s.Name == request.Name && s.Id != id && !s.Deleted);

            if (existingSpecialities?.Any() == true)
            {
                throw new ConflictException(ErrorCodes.VALIDATION_ERROR, "Ya existe una especialidad con ese nombre."); 
            }

            speciality.Update(request.Name, request.Description);

            var updatedSpeciality = await _persistence.Update(speciality);

            return new SpecialityModel.Response(updatedSpeciality.Id, updatedSpeciality.Name, updatedSpeciality.Description);
        }

        public async Task Delete(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id);

            if (speciality is null)
            {
                throw new EntityNotFoundException("Speciality");
            }

            speciality.Delete();

            await _persistence.Update(speciality);
        }

        private static void ValidateRequest(SpecialityModel.Request request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException("El nombre es obligatorio.", ErrorCodes.VALIDATION_ERROR).WithDetail("name", "El nombre es obligatorio.");
            }

            if (request.Name.Length < 3 || request.Name.Length > 100)
            {
                throw new ValidationException("El nombre debe tener entre 3 y 100 caracteres.", ErrorCodes.VALIDATION_ERROR).WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ValidationException("La descripción es obligatoria.", ErrorCodes.VALIDATION_ERROR).WithDetail("description", "La descripción es obligatoria.");
            }

            if (request.Description.Length < 10 || request.Description.Length > 100)
            {
                throw new ValidationException("La descripción debe tener entre 10 y 100 caracteres.", ErrorCodes.VALIDATION_ERROR).WithDetail("description", "Debe tener entre 10 y 100 caracteres.");
            }
        }
    }

}
