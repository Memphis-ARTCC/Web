using Memphis.Shared.Enums;
using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class ExamRequestDto
{
    public int Id { get; set; }
    public required string Instructor { get; set; }
    public required string Student { get; set; }
    public required Exam Exam { get; set; }
    public required string Reason { get; set; }
    public ExamRequestStatus Status { get; set; }
    public DateTimeOffset Created { get; set; }

    public static ExamRequestDto Parse(ExamRequest examRequest)
    {
        return new ExamRequestDto
        {
            Id = examRequest.Id,
            Instructor = $"{examRequest.Instructor.FirstName} {examRequest.Instructor.LastName}",
            Student = $"{examRequest.Student.FirstName} {examRequest.Student.LastName}",
            Exam = examRequest.Exam,
            Reason = examRequest.Reason,
            Status = examRequest.Status,
            Created = examRequest.Created
        };
    }

    public static IList<ExamRequestDto> ParseMany(IList<ExamRequest> examRequests)
    {
        return examRequests.Select(Parse).ToList();
    }
}
