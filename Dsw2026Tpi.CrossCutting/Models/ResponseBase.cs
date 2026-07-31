namespace Dsw2026Tpi.CrossCutting.Models;

public record ErrorResponse(string ErrorCode, string Message)
{
    public ICollection<ErrorDetail> Details { get; } = [];
    public void AddDetail(string field, string issue)
    {
        Details.Add(new ErrorDetail(field, issue));
    }
    public void AddDetail(IEnumerable<(string Field, string Issue)> details)
    {
        foreach (var detail in details)
        {
            AddDetail(detail.Field, detail.Issue);
        }
    }
}
public record ErrorDetail(string Field, string Issue);

public record SuccessResponse(bool Success = true);