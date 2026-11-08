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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetRanking([FromQuery] string? tier, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _rankingService.GetRankingAsync(tier, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

    }

}