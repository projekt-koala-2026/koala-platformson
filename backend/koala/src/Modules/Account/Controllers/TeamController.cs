using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/teams")]
    public class TeamController : ControllerBase
    {
        private readonly TeamService _teamService;

        public TeamController(TeamService teamService)
        {
            _teamService = teamService;
        }

        // ONLY FOR TEAM PLAYERS + (REQUIRED MOSTLY WITH CAPTAIN POSITION IN TEAM_MEMBERS)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequestDto requestDto)
        {
            var response = await _teamService.CreateTeamAsync(User, requestDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<TeamDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam([FromRoute] Guid id)
        {
            await _teamService.DeleteTeamAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpDelete("{id}/member/{team_member_id}")]
        public async Task<IActionResult> DeleteTeamMember([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            await _teamService.DeleteTeamMemberAsync(User, id, team_member_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        [Authorize]
        [HttpPut("{id}/name")]
        public async Task<IActionResult> UpdateTeamName([FromRoute] Guid id, [FromBody] UpdateTeamNameRequestDto requestDto)
        {
            var response = await _teamService.UpdateTeamNameAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<TeamDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        [Authorize]
        [HttpPost("{id}/new-join-code")]
        public async Task<IActionResult> CreateJoinTeamCode([FromRoute] Guid id)
        {
            var response = await _teamService.CreateJoinTeamCodeAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<TeamJoinCodeDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        // ONLY FOR TEAM ADMINS AND PLAYERS
        [Authorize]
        [HttpPost("join/{join_code}")]
        public async Task<IActionResult> JoinTeamWithCode([FromRoute] string join_code)
        {
            var response = await _teamService.JoinTeamWithCodeAsync(User, join_code); 
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<TeamMemberDto>>(true, DateTime.UtcNow, null, null, response)); 
        }
        //ONLY FOR TEAM PLAYERS AND TEAM AND ORGANIZATION ADMINS
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam([FromRoute] Guid id)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
        //ONLY FOR TEAM AND ORGANIZATION ADMINS
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTeams([FromRoute] PageQueryDto pageQueryDto)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

        [Authorize]
        [HttpPost("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> CreateTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

        [Authorize]
        [HttpPut("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> UpdateTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

        [Authorize]
        [HttpDelete("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> DeleteTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

        [Authorize]
        [HttpGet("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> GetTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }
    }
}