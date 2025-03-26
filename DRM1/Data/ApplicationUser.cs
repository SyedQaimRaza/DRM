using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace DRM.Data
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }

        public string? ProfileImage { get; set; }

        [StringLength(100)]
        public string? Designation { get; set; }

        private string? _uniqueKey;

        public string? UniqueKey
        {
            get => _uniqueKey != null ? DecryptUniqueKey(_uniqueKey) : null;
            set => _uniqueKey = value != null ? Encrypt(value) : null;
        }

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();

            byte[] keyBytes = Encoding.UTF8.GetBytes("DevelopedByAnonymous".PadRight(32, 'X'));
            aes.Key = keyBytes.Take(32).ToArray();
            aes.IV = new byte[16]; // 16-byte IV for AES-256

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        private static string DecryptUniqueKey(string cipherText)
        {
            using var aes = Aes.Create();

            byte[] keyBytes = Encoding.UTF8.GetBytes("DevelopedByAnonymous".PadRight(32, 'X'));
            aes.Key = keyBytes.Take(32).ToArray();
            aes.IV = new byte[16]; // 16-byte IV for AES-256

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] inputBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
