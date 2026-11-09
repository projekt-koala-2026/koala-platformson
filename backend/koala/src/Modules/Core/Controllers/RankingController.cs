using koala.src.Modules.Account.Entities;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Core.Controllers
{
    [ApiController]
    [Route("api/koala/core/ranking")]
    public class RankingController : ControllerBase
    {
        private readonly RankingService _rankingService;

        public RankingController(RankingService rankingService)
        {
            _rankingService = rankingService;
        }
        
        //TODO: dodac endpoint dal admina z generowaniem rankingu atomatycznego
        [Authorize]
        [HttpPost("generete/subedtion/{subedition_id}")]
        [HttpPost("generate/subedition/{subedition_id}")]
        public async Task<IActionResult> GenereteRankingFromSubEdtion([FromRoute] Guid subedition_id)
        {
            await _rankingService.GenereteRankingFromSubEdtionAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true , DateTime.UtcNow, null, null, null));
        }

        // [Authorize]
        // [HttpPut("subedtion/{subedition_id}")]
        // public async Task<IActionResult> UpdateRankingForSubEdtion([FromRoute] Guid subedition_id)
        // {
            
        // }

        // [Authorize]
        // [HttpPost("subedtion/{subedition_id}")]
        // public async Task<IActionResult> CreateRankingForSubEdtion([FromRoute] Guid subedition_id)
        // {
            
        // } 

        [Authorize]
        [HttpPut("subedition/{subedition_id}/publish")]
        public async Task<IActionResult> PublishRanking([FromRoute] Guid subedition_id)
        {
            await _rankingService.PublishRankingAsync(User, subedition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("subedition/{subedition_id}/draft")]
        public async Task<IActionResult> GetDraftRanking([FromRoute] Guid subedition_id, [FromQuery] string? tier, [FromQuery] PageQueryDto pageQueryDto)
        {
            if (!ClaimsHelper.IsRoleOrganizationAdmin(User))
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Only an administrator can view unpublished rankings");
            (var responseData, var responsePagination) = await _rankingService.GetRankingAsync(subedition_id, tier, pageQueryDto, true);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRanking([FromQuery] Guid subeditionId, [FromQuery] string? tier, [FromQuery] PageQueryDto pageQueryDto)
        {
            if (subeditionId == Guid.Empty)
                throw new KoalaException(KoalaErrorCodes.BadRequest, "A subedition id is required");
            (var responseData, var responsePagination) = await _rankingService.GetRankingAsync(subeditionId, tier, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }

}
