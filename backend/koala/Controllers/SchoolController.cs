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
    //FIXME: fix the fromroute validations :)
    [ApiController]
    [Route("api/admin/school")]
    public class AdminSchoolController : ControllerBase
    {
        public SchoolService _schoolService;

        public AdminSchoolController(SchoolService schoolService)
        {
            _schoolService = schoolService;
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("import/csv")]
        public async Task<IActionResult> AdminPanelImportSchoolsFromCSV([FromForm] SchoolFileCreate csvFile)
        {
            var schools = await _schoolService.ImportSchoolsFromCSV(csvFile);
            return Ok(schools);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPost("school")]
        public async Task<IActionResult> AdminPanelAddSchool([FromBody] SchoolCreateVM newSchool)
        {
            var schoolInfoVM = await _schoolService.AddSchool(newSchool);
            return Ok(schoolInfoVM);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("nameshort")]
        public async Task<IActionResult> AdminPanelEditSchoolNameShort([FromBody] SchoolEditNameShortVM editSchool)
        {
            var schoolInfoVM = await _schoolService.EditSchoolNameShort(editSchool);
            return Ok(schoolInfoVM);
        }

        [HttpPut("name")]
        public async Task<IActionResult> AdminPanelEditSchoolName([FromBody] SchoolEditNameVM editSchool)
        {
            var schoolInfoVM = await _schoolService.EditSchoolName(editSchool);
            return Ok(schoolInfoVM);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("school")]
        public async Task<IActionResult> AdminPanelDeleteSchool([FromBody] SchoolDeleteVM deleteSchool)
        {
            await _schoolService.DeleteSchool(deleteSchool);
            return Ok();
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("schools")]
        public async Task<IActionResult> AdminPanelDeleteSchools()
        {
            await _schoolService.DeleteSchools();
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("school")]
        public async Task<IActionResult> GetSchools()
        {
            var schools = await _schoolService.GetSchools();
            return Ok(schools);
        }
    }
}