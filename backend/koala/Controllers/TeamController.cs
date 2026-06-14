using koala.Services;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public class TeamsController : ControllerBase
    {
        private readonly TeamService _teamService;

        public TeamsController(TeamService teamService)
        {
            _teamService = teamService;
        }


        [Authorize(Roles = "ADMIN")]
        [HttpGet("/api/admin/teams")]
        public async Task<ActionResult<IEnumerable<TeamUpdateVM>>> GetTeams()
        {
            var teams = await _teamService.GetAllTeamsAsync();
            return Ok(teams);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("/api/admin/teams/{id}")]
        public async Task<IActionResult> AdminUpdateTeam(Guid id, [FromBody] TeamUpdateVM model)
        {
            if (id != model.Id)
            {
                return BadRequest(new { Message = "ID in URL path does not match the ID provided in the body." });
            }

            var currentTeam = await _teamService.GetTeamByIdAsync(id);
            if (currentTeam == null)
            {
                return NotFound(new { Message = $"Team with ID {id} does not exist." });
            }

            var updated = await _teamService.UpdateTeamAsync(id, model);
            if (!updated)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the team data." });
            }

            return NoContent();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("/api/admin/teams/{id}")]
        public async Task<IActionResult> AdminDeleteTeam(Guid id)
        {
            var currentTeam = await _teamService.GetTeamByIdAsync(id);
            if (currentTeam == null)
            {
                return NotFound(new { Message = $"Team with ID {id} not found." });
            }

            var deleted = await _teamService.DeleteTeamAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the team." });
            }

            return NoContent();
        }

        [Authorize(Roles = "CAPTAIN")]
        [HttpGet("my-team")]
        public async Task<ActionResult<TeamUpdateVM>> GetUserTeam()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var captainId))
            {
                return Unauthorized(new { Message = "Valid user identity not found in token." });
            }

            var team = await _teamService.GetTeamByCaptainIdAsync(captainId);
            if (team == null)
            {
                return NotFound(new { Message = "No team found where you are registered as the Captain." });
            }

            return Ok(team);
        }

        [Authorize(Roles = "CAPTAIN")]
        [HttpGet("{id}")]
        public async Task<ActionResult<TeamUpdateVM>> GetTeam(Guid id)
        {
            var team = await _teamService.GetTeamByIdAsync(id);
            if (team == null)
            {
                return NotFound(new { Message = $"Team with ID {id} not found." });
            }

            return Ok(team);
        }

        [Authorize(Roles = "CAPTAIN")]
        [HttpPost]
        public async Task<ActionResult<TeamUpdateVM>> CreateTeam([FromBody] TeamCreateVM model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var captainId))
            {
                return Unauthorized(new { Message = "Valid user identity not found in token." });
            }

            var existingTeam = await _teamService.GetTeamByCaptainIdAsync(captainId);
            if (existingTeam != null)
            {
                return BadRequest(new { Message = "You are already the Captain of an existing team." });
            }

            var createdTeam = await _teamService.CreateTeamAsync(model, captainId);
            return Ok(createdTeam);
        }

        [Authorize(Roles = "CAPTAIN")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] TeamUpdateVM model)
        {
            if (id != model.Id)
            {
                return BadRequest(new { Message = "ID in URL path does not match the ID provided in the body." });
            }

            var currentTeam = await _teamService.GetTeamByIdAsync(id);
            if (currentTeam == null)
            {
                return NotFound(new { Message = $"Team with ID {id} does not exist." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var updated = await _teamService.UpdateTeamAsync(id, model);
            if (!updated)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the team data." });
            }

            return NoContent();
        }

        [Authorize(Roles = "CAPTAIN")] 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(Guid id)
        {
            var currentTeam = await _teamService.GetTeamByIdAsync(id);
            if (currentTeam == null)
            {
                return NotFound(new { Message = $"Team with ID {id} not found." });
            }

            // Sprawdzenie, czy zalogowany użytkownik jest kapitanem TEJ drużyny
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var deleted = await _teamService.DeleteTeamAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the team." });
            }

            return NoContent();
        }
    }
}