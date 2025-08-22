using System.Text.Json.Serialization;

namespace ImageVault.Api.Models;

public class AppConfig
{
    [JsonPropertyName("dbHost")] public string DbHost { get; set; } = "";
    [JsonPropertyName("dbPort")] public int DbPort { get; set; } = 3306;
    [JsonPropertyName("dbName")] public string DbName { get; set; } = "";
    [JsonPropertyName("dbUser")] public string DbUser { get; set; } = "";
    [JsonPropertyName("dbPassword")] public string DbPassword { get; set; } = "";
}

