using Memphis.Shared.Enums;

namespace Memphis.Shared.Utils;

public static class Helpers
{
    public static string GetRatingName(Rating rating)
    {
        return rating switch
        {
            Rating.INAC => "Inactive",
            Rating.SUS => "Suspended",
            Rating.OBS => "Observer",
            Rating.S1 => "Student 1",
            Rating.S2 => "Student 2",
            Rating.S3 => "Student 3",
            Rating.C1 => "Controller 1",
            Rating.C2 => "Controller 2",
            Rating.C3 => "Controller 3",
            Rating.I1 => "Instructor 1",
            Rating.I2 => "Instructor 2",
            Rating.I3 => "Instructor 3",
            Rating.SUP => "Supervisor",
            Rating.ADM => "Administrator",
            _ => "Unknown",
        };
    }
}