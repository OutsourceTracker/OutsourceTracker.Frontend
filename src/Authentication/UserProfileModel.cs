namespace OutsourceTracker.Authentication;

public class UserProfileModel
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? AlphaCode { get; set; }
    public string? WorkdayId { get; set; }
    public string? Email { get; set; }

    // Local UI preferences (not yet persisted on backend)
    public bool EnableNotifications { get; set; } = true;
    public bool DarkMode { get; set; }
    public bool AutoSaveReports { get; set; }
    public bool EmailDailySummaries { get; set; }
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AlphaCode { get; set; }
    public string? WorkdayId { get; set; }
}