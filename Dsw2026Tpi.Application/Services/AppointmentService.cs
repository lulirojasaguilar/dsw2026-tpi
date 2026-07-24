using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IPersistence _persistence;

        public AppointmentService(IPersistence persistence)
        {
            _persistence = persistence;
        }
    }
}
