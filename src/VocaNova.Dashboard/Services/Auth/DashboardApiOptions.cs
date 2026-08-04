namespace VocaNova.Dashboard.Services.Auth;

/// <summary>Endpoint của VocaNova.API mà dashboard gọi qua HttpClient.</summary>
public sealed class DashboardApiOptions
{
    public const string SectionName = "VocaNovaApi";

    public string BaseUrl { get; set; } = "http://localhost:5013";
}
