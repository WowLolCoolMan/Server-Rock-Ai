namespace ServerRock.Core.Models;

public sealed class AppSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "llama3.2";
    public double Temperature { get; set; } = 0.7;
    public string? ProjectPath { get; set; }
    public string? ProjectInstructions { get; set; }
}
