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
            if (pageSize <= 0)
            {
                throw new ValidationException(
                    "El tamaño de página debe ser mayor que cero.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "pageSize",
                        "Debe ser mayor que cero.");
            }

            if (pageIndex < 0)
            {
                throw new ValidationException(
                    "El índice de página no puede ser negativo.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "pageIndex",
                        "No puede ser negativo.");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();

                if (name.Length < 3 || name.Length > 100)
                {
                    throw new ValidationException(
                        "El nombre debe tener entre 3 y 100 caracteres.",
                        nameof(ErrorCodes.VALIDATION_ERROR))
                        .WithDetail(
                            "name",
                            "Debe tener entre 3 y 100 caracteres.");
                }
            }

            var specialities = await _persistence.Paginate<Speciality, string>(pageSize, pageIndex, s =>!s.Deleted && (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)), s => s.Name);
            return specialities.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            request = NormalizeRequest(request);

            ValidateRequest(request);

            var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && !s.Deleted);

            if (existingSpeciality is not null)
            {
                throw new ConflictException(
                    "DUPLICATE_SPECIALITY_NAME", 
                    "Ya existe una especialidad con ese nombre.")
                    .WithDetail(
                    "name",
                    "El nombre de la especialidad ya se encuentra registrado.");     
            }

            var speciality = new Speciality(
                request.Name,
                request.Description);

            var createdSpeciality =
                await _persistence.Add(speciality);

            await _persistence.SaveChangesAsync();

            return new SpecialityModel.Response(
                createdSpeciality.Id,
                createdSpeciality.Name,
                createdSpeciality.Description);
        }

        public async Task<SpecialityModel.Response> Update(Guid id, SpecialityModel.Request request)
        {
            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    "El identificador de la especialidad es obligatorio.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "id",
                        "Debe indicar un identificador válido.");
            }

            request = NormalizeRequest(request);

            ValidateRequest(request);

            var speciality = await _persistence.GetById<Speciality>(id);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && s.Id != id && !s.Deleted);

            if (existingSpeciality is not null)
            {
                throw new ConflictException(
                    "DUPLICATE_SPECIALITY_NAME", 
                    "Ya existe una especialidad con ese nombre.")
                    .WithDetail( 
                    "name", 
                    "El nombre de la especialidad ya se encuentra registrado.");
            }

            speciality.Update(request.Name, request.Description);

            var updatedSpeciality = await _persistence.Update(speciality);
            
            await _persistence.SaveChangesAsync();

            return new SpecialityModel.Response(updatedSpeciality.Id, updatedSpeciality.Name, updatedSpeciality.Description);
        }

        public async Task Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ValidationException(
                    "El identificador de la especialidad es obligatorio.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "id",
                        "Debe indicar un identificador válido.");
            }

            var speciality = await _persistence.GetById<Speciality>(id);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            speciality.Delete();

            await _persistence.Update(speciality);
            await _persistence.SaveChangesAsync();
        }

        private static SpecialityModel.Request NormalizeRequest(
                SpecialityModel.Request request)
        {
            if (request is null)
            {
                throw new ValidationException(
                    "Los datos de la especialidad son obligatorios.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "request",
                        "Debe enviar los datos de la especialidad.");
            }

            return new SpecialityModel.Request(request.Name?.Trim() ?? string.Empty, request.Description?.Trim() ?? string.Empty);
        }

        private static void ValidateRequest(SpecialityModel.Request request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException(
                    "El nombre es obligatorio.", 
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "name",
                        "El nombre es obligatorio.");
            }

            if (request.Name.Length < 3 || request.Name.Length > 100)
            {
                throw new ValidationException(
                    "El nombre debe tener entre 3 y 100 caracteres.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "name",
                        "Debe tener entre 3 y 100 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ValidationException(
                    "La descripción es obligatoria.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "description",
                        "La descripción es obligatoria.");
            }

            if (request.Description.Length < 10 || request.Description.Length > 100)
            {
                throw new ValidationException(
                    "La descripción debe tener entre 10 y 100 caracteres.",
                    nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(
                        "description",
                        "Debe tener entre 10 y 100 caracteres.");
            }
        }
    }

}
