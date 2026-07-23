using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public long Dni { get; init; }

        public string Email { get; init; }

        public string ApplicationUserId { get; init; }

        public bool Deleted { get; private set; }

        #region Constructor for EF
#pragma warning disable CS8618
        private Patient()
        {
        }
#pragma warning restore CS8618
        #endregion

        public Patient(
            long dni,
            string email,
            string applicationUserId,
            Guid? id = null) : base(id)
        {
            Dni = dni;
            Email = email;
            ApplicationUserId = applicationUserId;
            Deleted = false;
        }

        public void Delete()
        {
            Deleted = true;
        }
    }
}
