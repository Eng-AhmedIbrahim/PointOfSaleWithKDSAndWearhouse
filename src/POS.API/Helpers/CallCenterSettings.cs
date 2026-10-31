namespace POS.API.Helpers;

public record CallCenterSettings
{
    public bool IsCentralCallCenter { get; set; }
    public bool IsDispatcher { get; set; }
    public int SendRetryIntervalMessagesAfterByMinutes { get; set; }
    public int MaxRetryAttempts { get; set; } = 100; // Maximum retry attempts before giving up
}
