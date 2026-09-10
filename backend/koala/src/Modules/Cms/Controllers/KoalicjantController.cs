using koala.src.Modules.Cms.Dtos;
using koala.src.Modules.Cms.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Cms.Controllers
{
    [ApiController]
    [Route("api/koala/cms/koalicjants")]
    public class KoalicjantController : ControllerBase
    {
        private readonly KoalicjantService _koalicjantService;

        public KoalicjantController(KoalicjantService koalicjantService)
        {
            _koalicjantService = koalicjantService;
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKoalicjant([FromRoute] Guid id, [FromBody] UpdateKoalicjantRequestDto requestDto)
        {
            var responseData = await _koalicjantService.UpdateKoalicjantAsync(User, id, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<KoalicjantDto>(true, DateTime.UtcNow, null, null, responseData));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetKoalicjants([FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _koalicjantService.GetKoalicjantsAsync(User, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWraper<List<KoalicjantDto>>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }
    }
}