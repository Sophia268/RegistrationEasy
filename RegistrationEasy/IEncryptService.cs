using System;
using System.Threading.Tasks;
namespace AutoDeployTool.Services;

public interface IEncryptService
{
    Task<EncryptionResult> EncryptFilesAsync(string inputFile, string outputFile, string? password = null);
    void EncryptFiles(string inputFile, string outputFile, string? password = null);
    void DecryptFiles(string inputFile, string outputFile, string? password = null);
}

public class EncryptionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long OriginalSize { get; set; }
    public long EncryptedSize { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Exception? Exception { get; set; }

    public static EncryptionResult CreateSuccess(string message, long originalSize, long encryptedSize)
    {
        return new EncryptionResult
        {
            Success = true,
            Message = message,
            OriginalSize = originalSize,
            EncryptedSize = encryptedSize,
            Timestamp = DateTime.Now
        };
    }

    public static EncryptionResult CreateFailure(string message, Exception? exception = null)
    {
        return new EncryptionResult
        {
            Success = false,
            Message = message,
            Exception = exception,
            Timestamp = DateTime.Now
        };
    }
}

public class EncryptionException : Exception
{
    public EncryptionException(string message, Exception? inner = null) : base(message, inner) { }
}