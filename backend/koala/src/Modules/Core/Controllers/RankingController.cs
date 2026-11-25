using koala.src.Modules.Account.Entities;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
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
        [HttpPost("edition/{edition_id}/generete/{Etap1Series_id}")]
        public async Task<IActionResult> CreateRankingsFromEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            await _rankingService.CreateRankingsFromEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPost("edition/{edition_id}/merge")]
        public async Task<IActionResult> Merge2RankingsFromEdition([FromRoute] Guid edition_id, [FromBody] RankingMergeRequestDto requestDto)
        {
            await _rankingService.Merge2RankingsFromEditionAsync(User, edition_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPost("edition/{edition_id}/new")]
        public async Task<IActionResult> CreateRankingManualy([FromRoute] Guid edition_id, [FromBody] RankingCreateDto requestDto)
        {
            await _rankingService.CreateRankingManualyAsync(User, edition_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpDelete("edition/{edition_id}/ranking/{ranking_id}")]
        public async Task<IActionResult> DeleteRanking([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id)
        {
            await _rankingService.DeleteRankingAsync(User, edition_id, ranking_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("edition/{edition_id}/ranking/{ranking_id}")]
        public async Task<IActionResult> UpdateRanking([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id, [FromBody] RankingUpdateDto requestDto)
        {
            var result = await _rankingService.UpdateRankingAsync(User, edition_id, ranking_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<RankingDto>(true, DateTime.UtcNow, null, null, result));
        }

        [Authorize]
        [HttpPut("edition/{edition_id}/ranking/{ranking_id}/make-public")]
        public async Task<IActionResult> MakeRankingPublic([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id)
        {
            await _rankingService.MakeRankingPublicAsync(User, edition_id, ranking_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("edition/{edition_id}/ranking/{ranking_id}/make-not-public")]
        public async Task<IActionResult> MakeRankingNotPublic([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id)
        {
            await _rankingService.MakeRankingNotPublicAsync(User, edition_id, ranking_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }


        [Authorize]
        [HttpPut("edition/{edition_id}/ranking/{ranking_id}/ranking-data/{ranking_data_id}")]
        public async Task<IActionResult> UpsertRankingData([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id, [FromRoute] Guid ranking_data_id, [FromBody] RankingDataUpsertDto requestDto)
        {
            var result = await _rankingService.UpsertRankingDataAsync(User, edition_id, ranking_id, ranking_data_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<RankingDataDto>(true, DateTime.UtcNow, null, null, result));
        }

        [Authorize]
        [HttpDelete("edition/{edition_id}/ranking/{ranking_id}/ranking-data/{ranking_data_id}")]
        public async Task<IActionResult> DeleteRankingData([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id, [FromRoute] Guid ranking_data_id)
        {
            await _rankingService.DeleteRankingDataAsync(User, edition_id, ranking_id, ranking_data_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [AllowAnonymous]
        [HttpGet("edition/{edition_id}")]
        public async Task<IActionResult> GetEditionRankings([FromRoute] Guid edition_id)
        {
            var result = await _rankingService.GetEditionRankingsAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDto>>(true, DateTime.UtcNow, null, null, result));
        }

        [AllowAnonymous]
        [HttpGet("edition/{edition_id}/ranking/{ranking_id}")]
        public async Task<IActionResult> GetEditionRankingData([FromRoute] Guid edition_id, [FromRoute] Guid ranking_id, [FromQuery] PageQueryDto queryDto)
        {
            (var result, var pagination) = await _rankingService.GetEditionRankingDataAsync(User, edition_id, ranking_id, queryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDataDto>>(true, DateTime.UtcNow, null, pagination, result));
        }

    }

}
