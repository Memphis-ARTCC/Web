using Memphis.Shared.Enums;

namespace Memphis.Shared.Models;

public class ExamRequest
{
    public int Id { get; set; }
    public required User Instructor { get; set; }
    public required User Student { get; set; }
    public required Exam Exam { get; set; }
    public required string Reason { get; set; }
    public ExamRequestStatus Status { get; set; } = ExamRequestStatus.PENDING;
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
