namespace VocaNova.API.Infrastructure.Sms;

public sealed class SpeedSmsSettings
{
    public const string SectionName = "SpeedSms";

    public bool Enabled { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public int SmsType { get; set; } = 5;

    public string BaseUrl { get; set; } = "https://api.speedsms.vn/index.php/";
}
