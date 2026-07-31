using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public Guid ApplicationUserId { get; private set; }

        public string Dni { get; private set; }

        public string? FullName { get; private set; }

        public bool Deleted { get; private set; }

        #region Constructor for EF
#pragma warning disable CS8618
        private Patient()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Patient(
            Guid applicationUserId,
            string dni,
            string? fullName = null,
            Guid? id = null) : base(id)
        {
            Validate(applicationUserId, dni, fullName);

            ApplicationUserId = applicationUserId;
            Dni = dni.Trim();
            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
            Deleted = false;
        }

        public void UpdateFullName(string? fullName)
        {
            if (!string.IsNullOrWhiteSpace(fullName) && fullName.Trim().Length > 150)
            {
                throw new ValidationException("El nombre completo no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("fullName", "No puede superar los 150 caracteres.");
            }

            FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        }

        public void Delete()
        {
            Deleted = true;
        }

        public static bool IsValidDni(string? dni, int minDigits = 7, int maxDigits = 10)
        {
            var normalized = dni?.Trim();
            return !string.IsNullOrWhiteSpace(normalized)
                && normalized.All(char.IsDigit)
                && normalized.Length >= minDigits
                && normalized.Length <= maxDigits;
        }

        private static void Validate(Guid applicationUserId, string dni, string? fullName)
        {
            if (applicationUserId == Guid.Empty)
            {
                throw new ValidationException("El usuario asociado al paciente no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("userId", "Es obligatorio.");
            }

            if (!IsValidDni(dni))
            {
                throw new ValidationException("El DNI indicado no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("dni", "Debe ser numérico, entre 7 y 10 dígitos.");
            }

            if (!string.IsNullOrWhiteSpace(fullName) && fullName.Trim().Length > 150)
            {
                throw new ValidationException("El nombre completo no es válido.", nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail("fullName", "No puede superar los 150 caracteres.");
            }
        }
    }
}
