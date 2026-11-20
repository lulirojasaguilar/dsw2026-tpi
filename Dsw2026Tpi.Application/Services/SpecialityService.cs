using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService
    {
        private readonly IPersistence _persistence;

        public SpecialityService(IPersistence persistence)
        {
            _persistence = persistence;
        }

        public async Task<Pagination<SpecialityModel.Response>> GetAll(
            int pageSize, 
            int pageIndex, 
            string? name = null)
        {
            RequestValidations.ValidatePagination(pageSize, pageIndex);

            var normalizedName = RequestValidations.NormalizeNameFilter(name);

            var specialities = await _persistence.Paginate<Speciality, string>(
                pageSize,
                pageIndex,
                s => !s.Deleted
                    && (normalizedName == null || s.Name.Contains(normalizedName)),
                s => s.Name);

            return specialities.Map(Map);
        }

        public async Task<SpecialityModel.Response> GetById(
            Guid id)
        {
            EnsureIdentifier(id);

            var speciality = await _persistence.GetById<Speciality>(id);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            return Map(speciality);
        }

        public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
        {
            request = NormalizeRequest(request);

            ValidateRequest(request);

            var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && !s.Deleted);

            if (existingSpeciality is not null)
            {
                throw DuplicateName();
            }

            var now = DateTime.UtcNow;
            var speciality = new Speciality(request.Name, request.Description);
            speciality.CreatedAt = now;
            speciality.UpdatedAt = now;

            var createdSpeciality = await _persistence.Add(speciality);

            try
            {
                await _persistence.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueConstraintViolation(ex))
            {
                throw DuplicateName();
            }

            return Map(createdSpeciality);
        }

        public async Task<SpecialityModel.Response> Update(
            Guid id, SpecialityModel.Request request)
        {
            EnsureIdentifier(id);

            request = NormalizeRequest(request);

            ValidateRequest(request);

            var speciality = await _persistence.GetById<Speciality>(id, nameof(Speciality.Doctors));

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            var existingSpeciality = await _persistence.First<Speciality>(s => s.Name == request.Name && s.Id != id && !s.Deleted);

            if (existingSpeciality is not null)
            {
                throw DuplicateName();
            }

            speciality.Update(request.Name, request.Description);
            speciality.UpdatedAt = DateTime.UtcNow;

            var updatedSpeciality = await _persistence.Update(speciality);
            await _persistence.SaveChangesAsync();

            return Map(updatedSpeciality);
        }

        public async Task Delete(Guid id)
        {
            EnsureIdentifier(id);

            var speciality = await _persistence.GetById<Speciality>(id, nameof(Speciality.Doctors));

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            if (speciality.HasDoctors)
            {
                // No se borra una especialidad con médicos asociados.
                throw new ConflictException(
                    ErrorCodes.SPECIALITY_HAS_DOCTORS,
                    nameof(ErrorCodes.SPECIALITY_HAS_DOCTORS));
            }

            speciality.Delete();
            speciality.UpdatedAt = DateTime.UtcNow;

            await _persistence.Update(speciality);
            await _persistence.SaveChangesAsync();
        }

        public async Task<IReadOnlyCollection<SpecialityModel.DoctorItem>> GetDoctors(Guid specialtyId)
        {
            EnsureIdentifier(specialtyId);

            var speciality = await _persistence.GetById<Speciality>(specialtyId);

            if (speciality is null || speciality.Deleted)
            {
                throw new EntityNotFoundException("Speciality");
            }

            var doctors = await _persistence.GetFiltered<Doctor>(
                doctor => doctor.SpecialityId == specialtyId
                    && !doctor.Deleted);

            var items = (doctors ?? [])
                .OrderBy(doctor => doctor.Name)
                .Select(doctor => new SpecialityModel.DoctorItem(doctor.Id, doctor.Name, doctor.LicenseNumber))
                .ToList();

            if (items.Count == 0)
            {
                // El front muestra "no hay médicos relacionados" a partir de este error.
                throw new EntityNotFoundException(
                    ErrorCodes.DOCTORS_NOT_FOUND,
                    nameof(ErrorCodes.DOCTORS_NOT_FOUND));
            }

            return items;
        }

        private static SpecialityModel.Response Map(Speciality speciality) => new(
            speciality.Id,
            speciality.Name,
            speciality.Description);

        private static AppException DuplicateName() =>
            new ConflictException(
                ErrorCodes.DUPLICATE_SPECIALTY_NAME,
                nameof(ErrorCodes.DUPLICATE_SPECIALTY_NAME))
                .WithDetail(
                    "name",
                    "El nombre de la especialidad ya se encuentra registrado.");

        private static void EnsureIdentifier(Guid id)
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

            return request with
            {
                Name = request.Name?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty
            };
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
