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
        [HttpGet]
        public async Task<IActionResult> GetRanking([FromQuery] string? tier, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _rankingService.GetRankingAsync(tier, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }

}