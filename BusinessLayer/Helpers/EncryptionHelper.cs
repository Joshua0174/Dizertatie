using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Helpers
{
    public static class EncryptionHelper
    {
        // ATENȚIE: Pentru disertație e ok hardcodat aici, 
        // dar în producție această cheie se ține în appsettings.json sau Azure Key Vault!
        // Trebuie să aibă EXACT 32 de caractere pentru AES-256 (256 biți).
        private static readonly byte[] Key = System.Text.Encoding.UTF8.GetBytes("kF?.Q]JNv}MpM$dyV$@X$!%8_3wSw3U5");

        public static byte[] Encrypt(byte[] plainBytes)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV(); // Vectorul de Inițializare (randomizează criptarea)

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream();

            // Salvăm IV-ul la începutul fișierului, avem nevoie de el la decriptare
            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            return memoryStream.ToArray(); // Returnează bytes criptați
        }

        public static byte[] Decrypt(byte[] encryptedBytes)
        {
            using var aes = Aes.Create();
            aes.Key = Key;

            // Extragem IV-ul (primele 16 bytes)
            var iv = new byte[16];
            Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream();

            // Decriptăm restul fișierului (sărind peste primele 16 bytes)
            using (var cryptoStream = new CryptoStream(new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16), decryptor, CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(memoryStream);
            }

            return memoryStream.ToArray(); // Returnează PDF-ul original
        }
    }
}
