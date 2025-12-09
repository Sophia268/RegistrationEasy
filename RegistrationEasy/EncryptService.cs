using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;
using System.Threading.Tasks;
namespace AutoDeployTool.Services;

public class EncryptService
{

    public static async Task<EncryptionResult> EncryptFilesAsync(string inputFile, string outputFile, string? password = null)
    {
        var encryptionPassword = password ?? ConfigProvider.Get().Password;

        Console.WriteLine("开始加密文件...");

        try
        {
            // 检查JSON文件是否存在
            if (!File.Exists(inputFile))
            {
                var message = $"未找到{inputFile}文件，跳过加密。";
                Console.WriteLine(message);
                return EncryptionResult.CreateFailure(message, new FileNotFoundException(message));
            }

            // 读取JSON文件内容
            var jsonContent = await File.ReadAllTextAsync(inputFile, Encoding.UTF8);
            var originalSize = jsonContent.Length;
            Console.WriteLine($"读取{inputFile}文件，大小：{originalSize} 字符");

            // 加密内容
            var encryptedContent = await Task.Run(() => EncryptAES(jsonContent, encryptionPassword));
            var encryptedSize = encryptedContent.Length;
            Console.WriteLine($"加密完成，加密后大小：{encryptedSize} 字符");

            // 写入加密文件
            await File.WriteAllTextAsync(outputFile, encryptedContent, Encoding.UTF8);
            Console.WriteLine($"[OK] 加密文件已保存为{outputFile}");

            return EncryptionResult.CreateSuccess("文件加密成功", originalSize, encryptedSize);
        }
        catch (Exception ex)
        {
            var message = $"加密过程中发生错误：{ex.Message}";
            Console.WriteLine($"[ERROR] {message}");
            return EncryptionResult.CreateFailure(message, ex);
        }
    }

    // 保持向后兼容的同步方法
    public static void EncryptFiles(string iFile, string oFile, string? password = null)
    {
        var result = EncryptFilesAsync(iFile, oFile, password).GetAwaiter().GetResult();
        if (!result.Success && result.Exception != null)
        {
            throw new EncryptionException(result.Message, result.Exception);
        }
    }

    public static void DecryptFiles(string iFile, string oFile, string? password = null)
    {
        var encryptionPassword = password ?? ConfigProvider.Get().Password;
        try
        {
            if (!File.Exists(iFile))
            {
                throw new FileNotFoundException($"未找到{iFile}文件");
            }
            var encryptedContent = File.ReadAllText(iFile, Encoding.UTF8);
            var decryptedContent = DecryptAES(encryptedContent, encryptionPassword);
            Console.WriteLine($"[OK] 文件解密成功");
            File.WriteAllText(oFile, decryptedContent, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 解密过程中发生错误：{ex.Message}");
            throw;
        }
    }

    private static string EncryptAES(string plainText, string password)
    {
        try
        {
            using var aes = Aes.Create();

            // 使用密码生成密钥和IV
            var key = GenerateKey(password, 32); // AES-256需要32字节密钥
            var iv = GenerateKey(password + "salt", 16); // AES需要16字节IV

            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            swEncrypt.Write(plainText);
            swEncrypt.Close();

            var encrypted = msEncrypt.ToArray();
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AES加密失败：{ex.Message}", ex);
        }
    }

    private static string DecryptAES(string cipherText, string password)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();

            // 使用相同的密码生成密钥和IV
            var key = GenerateKey(password, 32);
            var iv = GenerateKey(password + "salt", 16);

            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipherBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"AES解密失败：{ex.Message}", ex);
        }
    }

    private static byte[] GenerateKey(string password, int keySize)
    {
        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(passwordBytes);

        // 如果需要的密钥长度大于哈希长度，则重复哈希
        if (keySize <= hash.Length)
        {
            var key = new byte[keySize];
            Array.Copy(hash, key, keySize);
            return key;
        }
        else
        {
            var key = new byte[keySize];
            var offset = 0;
            while (offset < keySize)
            {
                var remaining = keySize - offset;
                var copyLength = Math.Min(remaining, hash.Length);
                Array.Copy(hash, 0, key, offset, copyLength);
                offset += copyLength;
            }
            return key;
        }
    }

    public static string EncryptText(string plainText, string? password = null)
    {
        var encryptionPassword = password ?? ConfigProvider.Get().Password;
        return EncryptAES(plainText, encryptionPassword);
    }

    public static string DecryptText(string cipherText, string? password = null)
    {
        var encryptionPassword = password ?? ConfigProvider.Get().Password;
        return DecryptAES(cipherText, encryptionPassword);
    }
}
