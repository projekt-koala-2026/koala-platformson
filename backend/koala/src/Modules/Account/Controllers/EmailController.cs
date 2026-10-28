using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace koala.src.Modules.Account.Controllers
{
    [ApiController]
    [Route("api/koala/account/emails")]
    public class EmailController : ControllerBase
    {
        private IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("/schools")]
        public async Task<IActionResult> SendBulkEmailToSchoolsAsync([FromBody] EmailSendRequestDto requestDto)
        {
            await _emailService.SendBulkEmailToSchoolsAsync(User, requestDto);
            return StatusCode(StatusCodes.Status200OK, new ApiResponseWrapper<object>(true, DateTime.UtcNow, null, null, null));
        }
    }
}