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
        [HttpPost("edition/{edition_id}/etap1-total-ranking")]
        public async Task<IActionResult> CreateTotalEtap1Rankings([FromRoute] Guid edition_id)
        {
            await _rankingService.GenereteEditionTotalRanking(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("edition/{edition_id}/total-ranking-make-public")]
        public async Task<IActionResult> MakeTotalRankingPublic([FromRoute] Guid edition_id)
        {
            await _rankingService.MakeTotalRankingPublicAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("edition/{edition_id}/total-ranking-make-not-public")]
        public async Task<IActionResult> MakeRankingNotPublic([FromRoute] Guid edition_id)
        {
            await _rankingService.MakeTotalRankingNotPublicAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("etap1series/{etap1series_id}/ranking-make-public")]
        public async Task<IActionResult> MakeSeriesRankingPublic([FromRoute] Guid etap1series_id)
        {
            await _rankingService.MakeSeriesRankingPublicAsync(User, etap1series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("etap1series/{etap1series_id}/ranking-make-not-public")]
        public async Task<IActionResult> MakeSeriesRankingNotPublic([FromRoute] Guid etap1series_id)
        {
            await _rankingService.MakeSeriesRankingNotPublicAsync(User, etap1series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }


        [Authorize]
        [HttpPut("ranking/{ranking_id}/ranking-data/{ranking_data_id}")]
        public async Task<IActionResult> UpsertRankingData([FromRoute] Guid ranking_id, [FromRoute] Guid ranking_data_id, [FromBody] RankingDataUpsertDto requestDto)
        {
            var result = await _rankingService.UpsertRankingDataAsync(User, ranking_id, ranking_data_id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<RankingDataDto>(true, DateTime.UtcNow, null, null, result));
        }

        [Authorize]
        [HttpDelete("ranking/{ranking_id}/ranking-data/{ranking_data_id}")]
        public async Task<IActionResult> DeleteRankingData([FromRoute] Guid ranking_id, [FromRoute] Guid ranking_data_id)
        {
            await _rankingService.DeleteRankingDataAsync(User, ranking_id, ranking_data_id);
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
            (var result, var pagination) = await _rankingService.GetEditionRankingDataAsync(User, ranking_id, edition_id, queryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDataDto>>(true, DateTime.UtcNow, null, pagination, result));
        }

        [AllowAnonymous]
        [HttpGet("etap1series/{etap1series_id}/ranking/{ranking_id}")]
        public async Task<IActionResult> GetEtap1SeriesRankingData([FromRoute] Guid etap1series_id, [FromRoute] Guid ranking_id, [FromQuery] PageQueryDto queryDto)
        {
            (var result, var pagination) = await _rankingService.GetEtap1SeriesRankingDataAsync(User, ranking_id, etap1series_id, queryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<List<RankingDataDto>>(true, DateTime.UtcNow, null, pagination, result));
        }

    }

}
