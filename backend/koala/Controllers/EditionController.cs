using koala.Data.ViewModels;
using koala.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/admin/edition")]
    public class AdminEditionController : ControllerBase
    {
        private readonly EditionService _editionService;

        public AdminEditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost] 
        public async Task<IActionResult> AdminPanelAddEdition([FromBody] EditionCreateVM newEdition)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var edition = await _editionService.AddEdition(newEdition);
            return Ok(edition);
        }


        [Authorize(Roles = "ADMIN")]
        [HttpPut("title")]
        public async Task<IActionResult> AdminPanelUpdateTitle([FromBody] EditionUpdateTitleVM updatedTitle)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _editionService.UpdateEditionTitle(updatedTitle);
            if (result == null)
            {
                return NotFound(new { message = $"Edycja o ID {updatedTitle.Id} nie istnieje." });
            }

            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("start-date")]
        public async Task<IActionResult> AdminPanelUpdateStartDate([FromBody] EditionUpdateStartDateVM updatedStartDate)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _editionService.UpdateEditionStartDate(updatedStartDate);
            if (result == null)
            {
                return NotFound(new { message = $"Edycja o ID {updatedStartDate.Id} nie istnieje." });
            }

            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("end-date")]
        public async Task<IActionResult> AdminPanelUpdateEndDate([FromBody] EditionUpdateEndDateVM updatedEndDate)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _editionService.UpdateEditionEndDate(updatedEndDate);
            if (result == null)
            {
                return NotFound(new { message = $"Edycja o ID {updatedEndDate.Id} nie istnieje." });
            }

            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> AdminPanelDeleteEdition([FromRoute] Guid id)
        {
            var deleted = await _editionService.DeleteEdition(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Edycja o ID {id} nie istnieje." });
            }

            return NoContent();
        }
        
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAllEditions()
        {
            var editions = await _editionService.GetAllEditions();

            return Ok(editions);
        }
    }

    [ApiController]
    [Route("api/edition")]
    public class EditionController : ControllerBase
    {
        private readonly EditionService _editionService;

        public EditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEditions()
        {
            var editions = await _editionService.GetAllEditions();
            
            return Ok(editions);
        }
    }
}