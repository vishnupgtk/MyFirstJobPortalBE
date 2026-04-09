using AuthSystemApi.DTOs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace AuthSystemApi.Services
{
    public class ResumeFileProcessingService
    {
        private readonly ResumeParsingService _resumeParsingService;

        public ResumeFileProcessingService(ResumeParsingService resumeParsingService)
        {
            _resumeParsingService = resumeParsingService;
        }

        public async Task<ResumeParseResponseDto> ProcessResumeFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            string extractedText;

            switch (fileExtension)
            {
                case ".pdf":
                    extractedText = await ExtractTextFromPdfAsync(file);
                    break;
                case ".docx":
                    extractedText = await ExtractTextFromDocxAsync(file);
                    break;
                case ".doc":
                    extractedText = await ExtractTextFromDocAsync(file);
                    break;
                case ".txt":
                    extractedText = await ExtractTextFromTxtAsync(file);
                    break;
                default:
                    throw new NotSupportedException($"File type {fileExtension} is not supported. Supported formats: PDF, DOCX, DOC, TXT");
            }

            if (string.IsNullOrWhiteSpace(extractedText))
                throw new InvalidOperationException("Could not extract text from the resume file");

            // Parse the extracted text using the existing parsing service
            return _resumeParsingService.ParseResume(extractedText);
        }

        private async Task<string> ExtractTextFromPdfAsync(IFormFile file)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var pdfReader = new PdfReader(stream);
                using var pdfDocument = new PdfDocument(pdfReader);

                var text = new StringBuilder();
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var pageText = PdfTextExtractor.GetTextFromPage(page);
                    text.AppendLine(pageText);
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error extracting text from PDF: {ex.Message}", ex);
            }
        }

        private async Task<string> ExtractTextFromDocxAsync(IFormFile file)
        {
            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var document = WordprocessingDocument.Open(stream, false);
                var body = document.MainDocumentPart?.Document?.Body;

                if (body == null)
                    return string.Empty;

                var text = new StringBuilder();
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error extracting text from DOCX: {ex.Message}", ex);
            }
        }

        private async Task<string> ExtractTextFromDocAsync(IFormFile file)
        {
            // For .doc files, we'll need a different approach or library
            // For now, throw an exception with guidance
            throw new NotSupportedException("DOC files are not currently supported. Please convert to DOCX or PDF format.");
        }

        private async Task<string> ExtractTextFromTxtAsync(IFormFile file)
        {
            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error reading text file: {ex.Message}", ex);
            }
        }

        public async Task<string> SaveResumeFileAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided");

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"resume_{userId}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return relative path for database storage
            return Path.Combine("uploads", "resumes", fileName).Replace("\\", "/");
        }

        public bool IsValidResumeFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return false;

            // Check file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                return false;

            return true;
        }
    }
}