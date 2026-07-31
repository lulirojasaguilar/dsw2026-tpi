using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    public string Name { get; private set; }

    public string? LicenseNumber { get; private set; }

    public bool Deleted { get; private set; }

    public Guid SpecialityId { get; private set; }

    public Speciality Speciality { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Doctor()
    {
    }
#pragma warning restore CS8618
    #endregion

    public Doctor(
        string name, 
        string? licenseNumber, 
        Speciality speciality, 
        Guid? id = null) : base(id)
    {
        Validate(name, licenseNumber, speciality);
       
        Name = name.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
        Deleted = false;
    }

    public void Update(
        string name, 
        string? licenseNumber, 
        Speciality speciality)
    {
        Validate(name, licenseNumber, speciality);

        Name = name.Trim();
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
    }

    public void Delete()
    {
        Deleted = true;

    }

    private static void Validate(
        string name,
        string? licenseNumber,
        Speciality speciality)
    {
        var normalizedName = name?.Trim() ?? string.Empty;

        if (normalizedName.Length is < 3 or > 100)
        {
            throw new ValidationException(
                "El nombre del médico no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "La longitud debe estar entre 3 y 100 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(licenseNumber) && licenseNumber.Trim().Length > 50)
        {
            throw new ValidationException(
                "El número de matrícula no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("licenseNumber", "No puede superar los 50 caracteres.");
        }

        if (speciality is null || speciality.Id == Guid.Empty)
        {
            throw new ValidationException(
                "La especialidad indicada no es válida.",
                nameof(ErrorCodes.SPECIALITY_NOT_FOUND))
                .WithDetail("specialityId", "Debe corresponder a una especialidad existente.");
        }
    }

}
