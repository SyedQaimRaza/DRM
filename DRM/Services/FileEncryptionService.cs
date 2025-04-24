using System.Security.Cryptography;
using System.Text;

namespace DRM.Services
{
    public interface IFileEncryptionService
    {
        byte[] EncryptFile(byte[] data);
        byte[] DecryptFile(byte[] encryptedData);
    }

    public class FileEncryptionService : IFileEncryptionService
    {
        public byte[] EncryptFile(byte[] data)
        {
            using Aes aes = Aes.Create();
            aes.Key = GenerateEncryptionKey();
            aes.GenerateIV(); // Use a random IV for encryption

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

            // Combine IV and Encrypted Data
            byte[] result = new byte[aes.IV.Length + encryptedData.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedData, 0, result, aes.IV.Length, encryptedData.Length);

            return result;
        }

        public byte[] DecryptFile(byte[] encryptedData)
        {
            using Aes aes = Aes.Create();
            aes.Key = GenerateEncryptionKey();

            // Extract IV (first 16 bytes)
            byte[] iv = new byte[16];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);

            // Extract encrypted content (remaining bytes)
            byte[] actualEncryptedData = new byte[encryptedData.Length - iv.Length];
            Buffer.BlockCopy(encryptedData, iv.Length, actualEncryptedData, 0, actualEncryptedData.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            return decryptor.TransformFinalBlock(actualEncryptedData, 0, actualEncryptedData.Length);
        }

        private byte[] GenerateEncryptionKey()
        {
            string key = "DRMOFFLINEKEY123";
            return Encoding.UTF8.GetBytes(key.PadRight(32, 'X').Substring(0, 32)); // Ensures a 32-byte key
        }
    }

}
