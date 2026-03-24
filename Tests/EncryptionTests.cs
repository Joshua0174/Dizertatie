using BusinessLayer.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    public class EncryptionTests
    {
        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalData()
        {
            // Arrange (Pregătirea datelor)
            // Simulăm conținutul unui PDF printr-un text
            string originalText = "Acesta este un document super secret pentru disertație.";
            byte[] originalBytes = Encoding.UTF8.GetBytes(originalText);

            // Act (Acțiunea)
            // 1. Criptăm datele
            byte[] encryptedBytes = EncryptionHelper.Encrypt(originalBytes);

            // 2. Le decriptăm la loc
            byte[] decryptedBytes = EncryptionHelper.Decrypt(encryptedBytes);

            // 3. Transformăm rezultatul înapoi în text ca să-l putem citi
            string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

            // Assert (Verificarea)

            // Ne asigurăm că procesul de criptare chiar a modificat datele (nu a scuipat același fișier)
            Assert.NotEqual(originalBytes, encryptedBytes);

            // Ne asigurăm că lungimea e diferită (din cauza vectorului de inițializare - IV)
            Assert.True(encryptedBytes.Length > originalBytes.Length);

            // CEL MAI IMPORTANT: Datele decriptate trebuie să fie absolut identice cu originalul!
            Assert.Equal(originalText, decryptedText);
        }
    }
}
