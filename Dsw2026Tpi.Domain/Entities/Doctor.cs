namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : EntityBase
{
    public string Name { get; private set; }
    public string LicenseNumber { get; private set; }
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

    public Doctor(string name, string licenseNumber, Speciality speciality, Guid? id = null) : base(id)
    {
        Name = name.Trim();
        LicenseNumber = licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
        Deleted = false; 
    }
    public void Update(string name, string licenseNumber, Speciality speciality)
    {
        Validate(name, licenseNumber, speciality);

        Name = name.Trim();
        LicenseNumber = licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
    }
    public void Delete() 
    {
        Deleted = true; 

    }

    private static void Validate(
    string name,
    string licenseNumber,
    Speciality speciality)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre del médico es obligatorio.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length < 3)
        {
            throw new ArgumentException(
                "El nombre del médico debe tener al menos 3 caracteres.",
                nameof(name));
        }

        if (normalizedName.Length > 100)
        {
            throw new ArgumentException(
                "El nombre del médico no puede superar los 100 caracteres.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new ArgumentException(
                "El número de matrícula es obligatorio.",
                nameof(licenseNumber));
        }

        if (licenseNumber.Trim().Length > 50)
        {
            throw new ArgumentException(
                "El número de matrícula no puede superar los 50 caracteres.",
                nameof(licenseNumber));
        }

        if (speciality is null)
        {
            throw new ArgumentNullException(
                nameof(speciality),
                "La especialidad es obligatoria.");
        }

        if (speciality.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "La especialidad debe tener un identificador válido.",
                nameof(speciality));
        }
    }

}
