namespace Memphis.Shared.Dtos;

public class CommentDto
{
    public int UserId { get; set; }
    public bool Confidential { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
}