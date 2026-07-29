using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Constants
{

    public static class AvailabilityStatuses
    {
        public const string Available = "AVAILABLE";
        public const string Booked = "BOOKED";
        public const string Blocked = "BLOCKED";

        public static readonly HashSet<string> All = new()
        {
            Available, Booked, Blocked
        };
    }
}
