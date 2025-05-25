using System.Text;

namespace xChain.Common;
using System.Security.Cryptography;

/// <summary>
/// Provides encryption and decryption utilities using AES and ECC.
/// </summary>
public static class EncryptionHelper
{
    /// <summary>
    /// Generates a random AES key and IV.
    /// </summary>
    public static (byte[] Key, byte[] IV) GenerateAesKey()
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        return (aes.Key, aes.IV);
    }

    /// <summary>
    /// Encrypts a message using AES.
    /// </summary>
    public static byte[] EncryptMessage(string message, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        return encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
    }

    /// <summary>
    /// Decrypts an AES-encrypted message.
    /// </summary>
    public static string DecryptMessage(byte[] encryptedMessage, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}