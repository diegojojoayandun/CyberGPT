namespace CyberGPT.API.Services;
public interface IOllamaService
{
    Task<string> GenerateAsync(string prompt, string context = "");
}
