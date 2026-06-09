using koala.Services;
using koala.Data.ViewModels;
using koala.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/admin/sponsors")]
    public class SponsorController : ControllerBase
    {
        private readonly SponsorService _sponsorService;

        public SponsorController(SponsorService sponsorService)
        {
            _sponsorService = sponsorService;
        }

        [HttpPost]
        public async Task<ActionResult<Sponsor>> AddSponsor([FromBody] SponsorCreateVM newSponsor)
        {
            var sponsor = await _sponsorService.CreateAsync(newSponsor);
            return sponsor == null ? NotFound() : Ok(sponsor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditSponsor(Guid id, [FromBody] SponsorUpdateVM editionSponsor)
        {
            var updated = await _sponsorService.UpdateAsync(id, editionSponsor);
            if (!updated)
                return NotFound($"Sponsor o ID {id} nie istnieje.");

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSponsor(Guid id)
        {
            if (!await _sponsorService.DeleteAsync(id))
                return NotFound();

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sponsor>>> GetSponsors()
        {
            var sponsors = await _sponsorService.GetAllAsync();
            return Ok(sponsors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sponsor>> GetSponsor(Guid id)
        {
            var sponsor = await _sponsorService.GetByIdAsync(id);
            
            if (sponsor == null) 
                return NotFound($"Sponsor o ID {id} nie istnieje.");

            return Ok(sponsor);
        }
    }
}