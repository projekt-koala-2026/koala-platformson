using koala.Services;
using koala.Data.ViewModels;
using koala.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/regulamin")]
    public class RegulaminController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _filePath;
        private const string ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DownloadName = "Regulamin_do_edycji.docx";

        public RegulaminController(IWebHostEnvironment env)
        {
            _env = env;
            _filePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Assets", "regulamin_wzor.docx"));
        }

        [HttpGet]
        public IActionResult GetRegulamin()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "regulamin.pdf");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("The regulations file was not found.");
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            return File(fileStream, "application/pdf", "regulamin.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> SaveTerms(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Nie przesłano żadnego pliku." });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".docx")
            {
                return BadRequest(new { message = "Niepoprawny format pliku. Dozwolone są tylko pliki .docx" });
            }

            try
            {
                var directoryPath = Path.GetDirectoryName(_filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { message = "Regulamin został pomyślnie zaktualizowany." });
            }
            catch (IOException ex)
            {
                return StatusCode(500, new { message = "Błąd zapisu pliku. Plik może być obecnie używany.", details = ex.Message });
            }
        }
    }
}