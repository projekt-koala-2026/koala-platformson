using koala.Services;
using koala.Data.ViewModels;
using koala.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/admin/koalicjants")]
    public class KoalicjantController : ControllerBase
    {
        private readonly KoalicjantService _koalicjantService;

        public KoalicjantController(KoalicjantService koalicjantService)
        {
            _koalicjantService = koalicjantService;
        }

        [HttpPost]
        public async Task<ActionResult<Koalicjant>> AddKoalicjant([FromBody] KoalicjantCreateVM newKoalicjant)
        {
            var koalicjant = await _koalicjantService.CreateAsync(newKoalicjant);
            
            return koalicjant == null ? NotFound() : Ok(koalicjant);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditKoalicjant(Guid id, [FromBody] KoalicjantUpdateVM editionKoalicjant)
        {
            var updated = await _koalicjantService.UpdateAsync(id, editionKoalicjant);
            if (!updated)
                return NotFound($"Koalicjant o ID {id} nie istnieje.");

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKoalicjant(Guid id)
        {
            if (!await _koalicjantService.DeleteAsync(id))
                return NotFound();

            return Ok();
        }

        [HttpGet]

        public async Task<ActionResult<IEnumerable<Koalicjant>>> GetKoalicjants()
        {
            var koalicjants = await _koalicjantService.GetAllAsync();
            if (koalicjants == null || !koalicjants.Any())
            {
                return NotFound("Nie znaleziono żadnych koalicjantów.");
            }
            return Ok(koalicjants);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Koalicjant>> GetKoalicjant(Guid id)
        {
            var koalicjant = await _koalicjantService.GetByIdAsync(id);
            
            if (koalicjant == null) 
                return NotFound($"Koalicjant o ID {id} nie istnieje.");

            return Ok(koalicjant);
        }
    }
}