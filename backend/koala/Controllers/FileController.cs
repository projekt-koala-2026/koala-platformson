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
    [Route("api/admin/file")]
    public class AdminFileController : ControllerBase
    {
        public FileService _fileService;

        public AdminFileController(FileService fileService)
        {
            _fileService = fileService;
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPost("public/file")]
        public async Task<IActionResult> AdminPanelSavePublicFile([FromForm] FileCreateVM newFile)
        {
            var file = await _fileService.SavePublicFile(newFile.Title, newFile.File);
            return Ok(file);
        }

        [AllowAnonymous]
        [HttpGet("public/files")]
        public async Task<IActionResult> GetPublicFilesList()
        {
            var files = await _fileService.ListPublicFiles();
            return Ok(files);
        }

        [AllowAnonymous]
        [HttpDelete("public/files")]
        public async Task<IActionResult> DeletePublicFile([FromBody] FileDeleteVM deleteFile)
        {
            await _fileService.DeletePublicFile(deleteFile.Id);
            return Ok("succes");
        }
    }

    [ApiController]
    [Route("api/file")]
    public class FileController : ControllerBase
    {
        public FileService _fileService;

        public FileController(FileService fileService)
        {
            _fileService = fileService;
        }


        [AllowAnonymous]
        [HttpGet("public/files")]
        public async Task<IActionResult> GetPublicFilesList()
        {
            var files = await _fileService.ListPublicFiles();
            return Ok(files);
        }

    }
}