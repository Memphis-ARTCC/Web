namespace Memphis.Shared.Models;

public class TransferRequestHistory
{
    public required string From { get; set; }
    public required string To { get; set; }
    public int Status { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset Created { get; set; }
}

public class TransferRequest
{
    public int Id { get; set; }
    public int Cid { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string From { get; set; }
    public string? Reason { get; set; }
    public required IList<TransferRequestHistory> TransferHistory { get; set; }
    public DateTimeOffset Submitted { get; set; }
}
