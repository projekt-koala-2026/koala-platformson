using koala.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using koala.Data.ViewModels;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace koala.Controllers
{
    //FIXME: make sure params are passed corectly (VALIDATION!!!!)
    [ApiController]
    [Route("api/admin/edition")]
    public class AdminEditionController : ControllerBase
    {
        public EditionService _editionService;

        public AdminEditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("edition")]
        public async Task<IActionResult> AdminPanelAddEdition([FromBody] EditionCreateVM newEdition)
        {
            var edition = await _editionService.AddEdition(newEdition);
            return Ok(edition);
        }
    }

    [ApiController]
    [Route("api/edition")]
    public class EditionController : ControllerBase
    {
        public EditionService _editionService;

        public EditionController(EditionService editionService)
        {
            _editionService = editionService;
        }

    }
}