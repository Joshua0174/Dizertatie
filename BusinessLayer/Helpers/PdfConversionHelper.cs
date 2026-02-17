using Microsoft.AspNetCore.Http;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Helpers
{
    public static class PdfConversionHelper
    {
        public static async Task<byte[]> ConvertToPdfAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (extension == ".pdf")
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return ms.ToArray();
            }

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp")
            {

                return ConvertImageToPdf(file);
            }

            throw new NotSupportedException("Only PDF conversion is supported in this helper.");
        }




        private static byte[] ConvertImageToPdf(IFormFile imageFile)
        {
            // 1. Copiem IFormFile într-un MemoryStream pentru a putea lucra cu el
            using (var memoryStream = new MemoryStream())
            {
                imageFile.CopyTo(memoryStream);
                memoryStream.Position = 0; // IMPORTANT: Resetăm poziția la început după copiere

                var document = new PdfDocument();
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);

                // --- AICI ESTE FIX-UL ---
                // Nu folosim () => new MemoryStream(...), ci pasăm stream-ul direct.
                // PDFSharp va citi direct din memoryStream.
                using (var image = XImage.FromStream(memoryStream))
                {
                    // Logica de redimensionare (este corectă)
                    double pageRatio = page.Width / page.Height;
                    double imageRatio = (double)image.PixelWidth / (double)image.PixelHeight;
                    double width, height;

                    if (imageRatio > pageRatio)
                    {
                        // Imaginea e mai lată decât pagina -> o potrivim la lățime
                        width = page.Width;
                        height = width / imageRatio;
                    }
                    else
                    {
                        // Imaginea e mai înaltă decât pagina -> o potrivim la înălțime
                        height = page.Height;
                        width = height * imageRatio;
                    }

                    // Desenăm imaginea centrată sau de la 0,0 (cum ai tu)
                    gfx.DrawImage(image, 0, 0, width, height);
                }

                // Salvăm PDF-ul rezultat într-un nou stream și returnăm byte array-ul
                using (var outputStream = new MemoryStream())
                {
                    document.Save(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }
}
