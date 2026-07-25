using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {

        var doctors = await _persistence.Paginate<Doctor, string>(
            pageSize, pageIndex,
            d => !d.Deleted && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)), // <- fix
            x => x.Name,
            nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber, new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        ValidateRequest(request);

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);

        var createdDoctor = await _persistence.Add(doctor);

        return new DoctorModel.Response(createdDoctor.Id, createdDoctor.Name, createdDoctor.LicenseNumber, new DoctorModel.SpecialityDto( speciality.Id, speciality.Name));
    }
    public async Task<DoctorModel.Response> Update(Guid id, DoctorModel.Request request)
    {
        ValidateRequest(request);

        var doctor = await _persistence.GetById<Doctor>(id, nameof(Doctor.Speciality));

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);

        if (speciality is null || speciality.Deleted)
        {
            throw new EntityNotFoundException("Speciality");
        }

        doctor.Update(request.Name, request.LicenseNumber, speciality);

        var updatedDoctor = await _persistence.Update(doctor);

        return new DoctorModel.Response(updatedDoctor.Id, updatedDoctor.Name, updatedDoctor.LicenseNumber, new DoctorModel.SpecialityDto(speciality.Id,  speciality.Name));
    }

    public async Task Delete(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);

        if (doctor is null || doctor.Deleted)
        {
            throw new EntityNotFoundException("Doctor");
        }

        doctor.Delete();

        await _persistence.Update(doctor);
    }
    private static void ValidateRequest(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("El nombre es obligatorio.",ErrorCodes.VALIDATION_ERROR).WithDetail("name","El nombre es obligatorio.");
        }

        if (request.Name.Length < 3 || request.Name.Length > 100)
        {
            throw new ValidationException("El nombre debe tener entre 3 y 100 caracteres.", ErrorCodes.VALIDATION_ERROR).WithDetail("name", "Debe tener entre 3 y 100 caracteres.");
        }

        if (request.SpecialityId == Guid.Empty)
        {
            throw new ValidationException("La especialidad es obligatoria.", ErrorCodes.VALIDATION_ERROR).WithDetail( "specialityId", "Debe indicar una especialidad válida.");
        }
    }
}
