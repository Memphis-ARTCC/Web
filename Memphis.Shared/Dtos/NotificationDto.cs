using Memphis.Shared.Models;

namespace Memphis.Shared.Dtos;

public class NotificationDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Link { get; set; }
    public bool Read { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public static NotificationDto Parse(Notification notification)
    {
        return new NotificationDto
        {
            Title = notification.Title,
            Link = notification.Link,
            Read = notification.Read,
            Timestamp = notification.Timestamp
        };
    }

    public static IList<NotificationDto> ParseMany(IList<Notification> notifications)
    {
        return notifications.Select(NotificationDto.Parse).ToList();
    }
}