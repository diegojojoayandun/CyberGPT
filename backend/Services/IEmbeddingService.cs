namespace CyberGPT.API.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}
