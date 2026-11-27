using koala.src.Modules.Account.Entities;
using koala.src.Modules.Core.Dtos;
using koala.src.Modules.Core.Services;
using koala.src.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace koala.src.Modules.Core.Controllers
{
    [ApiController]
    [Route("api/koala/core/editions")]
    public class EditionController : ControllerBase
    {
        private readonly EditionService _editionService;

        public EditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAndStartEdition([FromBody] EditionCreateDto createEditionDto)
        {
            var response = await _editionService.CreateEditionAsync(User, createEditionDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}")]
        public async Task<IActionResult> UpdateEdition([FromRoute] Guid edition_id, [FromBody] EditionUpdateDto editionUpdateDto)
        {
            var response = await _editionService.UpdateEditionAsync(User, edition_id, editionUpdateDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/activate")]
        public async Task<IActionResult> EditionActivate([FromRoute] Guid edition_id)
        {
            var response = await _editionService.EditionActivateAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/close")]
        public async Task<IActionResult> EditionClose([FromRoute] Guid edition_id)
        {
            var response = await _editionService.EditionCloseAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetEditions([FromQuery] PageQueryDto pageQueryDto, [FromQuery] EditionQueryDto editionQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEditionsAsync(User, pageQueryDto, editionQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [AllowAnonymous]
        [HttpGet("active-edition")]
        public async Task<IActionResult> GetActiveEdition()
        {
            var response = await _editionService.GetActiveEditionAsync(User);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));

        }

        [Authorize]
        [HttpPost("{edition_id}/Etap1Series")]
        public async Task<IActionResult> CreateEtap1Series([FromRoute] Guid edition_id, [FromBody] Etap1SeriesCreateDto createEtap1SeriesDto)
        {
            var response = await _editionService.CreateEtap1SeriesAsync(User, edition_id, createEtap1SeriesDto);
            return StatusCode(StatusCodes.Status201Created, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> UpdateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id, [FromBody] Etap1SeriesUpdateDto updateEtap1SeriesDto)
        {
            var response = await _editionService.UpdateEtap1SeriesAsync(User, edition_id, Etap1Series_id, updateEtap1SeriesDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> GetEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.GetEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [AllowAnonymous]
        [HttpGet("{edition_id}/Etap1Series")]
        public async Task<IActionResult> GetEtap1Series([FromRoute] Guid edition_id, [FromQuery] PageQueryDto pageQueryDto)
        {
            (var responseData, var responsePagination) = await _editionService.GetEtap1SeriesAsync(User, edition_id, pageQueryDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, responsePagination, responseData));
        }

        [Authorize]
        [HttpPut("{edition_id}/activate_Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> ActivateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.ActivateEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpPut("{edition_id}/close_Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> DeactivateEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            var response = await _editionService.CloseEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/Etap1Series/{Etap1Series_id}")]
        public async Task<IActionResult> DeleteEtap1Series([FromRoute] Guid edition_id, [FromRoute] Guid Etap1Series_id)
        {
            await _editionService.DeleteEtap1SeriesAsync(User, edition_id, Etap1Series_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpPut("{edition_id}/tasks/file/window-time")]
        public async Task<IActionResult> UpdateEditionTaskFileWindowTime([FromRoute] Guid edition_id, [FromBody] EditionTaskFileUpserTimeWindoDto request)
        {
            var response = await _editionService.UpsertEditionTaskFileTimeWindowAsync(User, edition_id, request);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<EditionDto>(true, DateTime.UtcNow, null, null, response));
        }

        [Authorize]
        [HttpDelete("{edition_id}/tasks/file")]
        public async Task<IActionResult> DeleteEditionTasksFile([FromRoute] Guid edition_id)
        {
            await _editionService.DeleteEditionTaskFileAsync(User, edition_id);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }

        [Authorize]
        [HttpGet("{edition_id}/tasks/file")]
        public async Task<IActionResult> GetEditionTasksFile([FromRoute] Guid edition_id)
        {
            var physicalPath = await _editionService.GetEditionTaskFileAsync(User, edition_id);
            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(physicalPath, out var contentType);
            return PhysicalFile(physicalPath, contentType ?? "application/octet-stream");
        }

        [Authorize]
        [HttpPut("{edition_id}/tasks/file")]
        public async Task<IActionResult> UpsertEditionTasksFile([FromRoute] Guid edition_id, [FromForm] EditionTaskFileUpserDto request)
        {
            var physicalPath = await _editionService.UpsertEditionTaskFileAsync(User, edition_id, request);
            var provider = new FileExtensionContentTypeProvider();
            provider.TryGetContentType(physicalPath, out var contentType);
            return PhysicalFile(physicalPath, contentType ?? "application/octet-stream");
        }

    }
}
