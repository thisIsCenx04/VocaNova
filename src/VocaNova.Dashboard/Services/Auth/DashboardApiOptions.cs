namespace VocaNova.Dashboard.Services.Auth;

public sealed class DashboardApiOptions
{
    public const string SectionName = "VocaNovaApi";

    public string BaseUrl { get; set; } = "http://localhost:5013";
}
