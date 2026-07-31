using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Domain.Entities;

public class Speciality : EntityBase
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    public bool Deleted { get; private set; }

    #region Constructor for EF

    #pragma warning disable CS8618
    private Speciality() { }
    #pragma warning restore CS8618

    #endregion

    public Speciality(
        string name, 
        string description, 
        Guid? id = null) : base(id)
    {
        Validate(name, description);

        Name = name.Trim();
        Description = description.Trim();
        Deleted = false;
    }

    public void Update(string name, string description)
    {
        Validate(name, description);

        Name = name.Trim();
        Description = description.Trim();
    }

    public void Delete()
    {
        Deleted = true;
    }

    public static void ValidateInput(string? name, string? description) => Validate(name!, description!);

    private static void Validate(string name, string description)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        var normalizedDescription = description?.Trim() ?? string.Empty;

        if (normalizedName.Length is < 3 or > 100)
        {
            throw new ValidationException(
                "El nombre de la especialidad no es válido.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("name", "La longitud debe estar entre 3 y 100 caracteres.");
        }

        if (normalizedDescription.Length is < 10 or > 100)
        {
            throw new ValidationException(
                "La descripción de la especialidad no es válida.",
                nameof(ErrorCodes.VALIDATION_ERROR))
                .WithDetail("description", "La longitud debe estar entre 10 y 100 caracteres.");
        }
    }
}
