using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/teams")]
    public class TeamController : ControllerBase
    {
        private readonly TeamService _teamService;
        private readonly RodoService _rodoService;

        public TeamController(TeamService teamService, RodoService rodoService)
        {
            _teamService = teamService;
            _rodoService = rodoService;
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
        // ONLY FOR TEAM PLAYERS + (REQUIRED MOSTLY WITH CAPTAIN POSITION IN TEAM_MEMBERS)
        [Authorize]
        [HttpPost("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> CreateTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id, [FromBody] RodoCreateDto rodoCreateDto)
        {
            var response = await _rodoService.AddRodoAsync(User, id, team_member_id, rodoCreateDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<RodoDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        [Authorize]
        [HttpPut("{id}/rodos/{team_member_id}/file")]
        public async Task<IActionResult> UpdateTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id, [FromBody] RodoUpdateDto rodoUpdateDto)
        {
            var response = await _rodoService.UpdateRodoAsync(User, id, team_member_id, rodoUpdateDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<RodoDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        [Authorize]
        [HttpDelete("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> DeleteTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid team_member_id)
        {
            await _rodoService.DeleteRodoAsync(User, id, team_member_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null)); 
        }

        // ONLY FOR TEAM ADMINS AND PLAYERS
        [Authorize]
        [HttpPost("join/{join_code}")]
        public async Task<IActionResult> JoinTeamWithCode([FromRoute] string join_code)
        {
            var response = await _teamService.JoinTeamWithCodeAsync(User, join_code); 
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<TeamMemberDto>>(true, DateTime.UtcNow, null, null, response)); 
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyTeams([FromQuery] PageQueryDto pageQueryDto, [FromQuery] TeamQueryDto teamQueryDto)
        {
            (var responseData, var responsePagination) = await _teamService.GetMyTeamsAsync(User, pageQueryDto, teamQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<TeamDto>>(true, DateTime.UtcNow, null, responsePagination, responseData)); 
        }
        //ONLY FOR TEAM PLAYERS AND TEAM ADMINS AND ORGANIZATION ADMINS
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam([FromRoute] Guid id)
        {
            var response = await _teamService.GetTeamAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<TeamDto>(true, DateTime.UtcNow, null, null, response)); 
        }
        //ONLY FOR TEAM AND ORGANIZATION ADMINS

        [Authorize]
        [HttpGet("{id}/rodos")]
        public async Task<IActionResult> GetTeamRodos([FromRoute] Guid id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _rodoService.GetRodosAsync(User, id,pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<RodoDto>>(true, DateTime.UtcNow, null, responsePagination, responseData)); 
        }
        [Authorize]
        [HttpGet("{id}/rodos/{team_member_id}")]
        public async Task<IActionResult> GetTeamMemberRodo([FromRoute] Guid id, [FromRoute] Guid teamMemberId)
        {
            var response = await _rodoService.GetRodoAsync(User, id, teamMemberId);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<RodoDto>(true, DateTime.UtcNow, null, null, response));

        }
        [Authorize]
        [HttpGet("{id}/rodos/{team_member_id}/file")]
        public async Task<IActionResult> GetTeamMemberRodoFile([FromRoute] Guid id, [FromRoute] Guid teamMemberId)
        {
            string physicalPath = await _rodoService.GetRodoFileAsync(User, id, teamMemberId);
            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(physicalPath, out var contentType);
            return PhysicalFile(physicalPath, contentType ?? "application/octet-stream");
        }

        //ONLY FOR ORGANIZATION ADMINS
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTeams([FromQuery] PageQueryDto pageQueryDto, [FromQuery] TeamQueryDto teamQueryDto, [FromQuery] TeamMemberQueryDto teamMemberQueryDto)
        {
            (var responseData, var responsePagination) = await _teamService.GetTeamsAsync(User, pageQueryDto, teamQueryDto, teamMemberQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<TeamDto>>(true, DateTime.UtcNow, null, responsePagination, responseData)); 
        }

        [Authorize]
        [HttpPut("{id}/rodos/{team_member_id}/state")]
        public async Task<IActionResult> SetTeamMemberRodoState([FromRoute] Guid id, [FromRoute] Guid teamMemberId, [FromBody] RodoStateUpdateDto rodoStateUpdateDto)
        {
            var response = await _rodoService.SetRodoStateAsync(User, id, teamMemberId, rodoStateUpdateDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<RodoDto>(true, DateTime.UtcNow, null, null, response));
        }
    }
}