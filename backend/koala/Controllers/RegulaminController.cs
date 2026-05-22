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

        public RegulaminController(IWebHostEnvironment env)
        {
            _env = env;
            _filePath = Path.Combine(_env.ContentRootPath, "Assets", "regulamin_wzor.docx");
        }

        [HttpGet("edit")]
        public IActionResult GetTermsForEdit()
        {
            if (!System.IO.File.Exists(_filePath))
            {
                return NotFound(new { message = $"Plik regulaminu nie istnieje na serwerze pod ścieżką: {_filePath}" });
            }

            const string downloadName = "Regulamin_do_edycji.docx";
            const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return PhysicalFile(_filePath, contentType, downloadName);
        }

        [HttpPost("save")]
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
                if (!Directory.Exists(directoryPath) && directoryPath != null)
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
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