using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Cms.Controllers
{
    [ApiController]
    [Route("api/koala/cms/sponsors")]
    public class SponsorController : ControllerBase
    {
        private readonly SponsorService _sponsorService;

        public SponsorController(SponsorService sponsorService)
        {
            _sponsorService = sponsorService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSponsor([FromBody] CreateSponsorRequestDto requestDto)
        {
            var responseData = await _sponsorService.CreateSponsorAsync(User, requestDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWraper<SponsorDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSponsor([FromRoute] Guid id)
        {
            await _sponsorService.DeleteSponsorAsync(User, id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSponsor([FromRoute] Guid id, [FromBody] UpdateSponsorRequestDto requestDto)
        {
            var responseData = await _sponsorService.UpdateSponsorAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<SponsorDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSponsors([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _sponsorService.GetSponsorsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<SponsorDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }
    }
}