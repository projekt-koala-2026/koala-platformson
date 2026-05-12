using koala.Services;
using koala.Data.ViewModels;
using koala.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegulaminController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public RegulaminController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("edycja")]
        public IActionResult GetRegulaminDoEdycji()
        {
            // Upewnij się, że folder Assets istnieje w głównym katalogu projektu
            // i zawiera plik regulamin_wzor.docx
            var filePath = Path.Combine(_env.ContentRootPath, "Assets", "regulamin_wzor.docx");

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "Plik regulaminu nie istnieje na serwerze pod ścieżką: " + filePath });
            }

            var fileName = "Regulamin_do_edycji.docx";
            var contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return PhysicalFile(filePath, contentType, fileName);
        }
    }
}