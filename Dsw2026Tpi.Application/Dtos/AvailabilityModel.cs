namespace Dsw2026Tpi.Application.Dtos
{
 
    public record AvailabilityModel
    {
        public record DayRequest(
            string Day,        
            string StartTime, 
            string EndTime     
        );

        public record Request(
            Guid DoctorId,
            List<DayRequest> Days
        );
      
        public record RuleSummary(
            Guid Id,
            string Day,
            string StartTime,
            string EndTime
        );

        public record Response(
            Guid DoctorId,
            int Month,
            int Year,
            int RulesCreated,
            int SlotsCreated,
            List<RuleSummary> Rules
        );
    }
}
