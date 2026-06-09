using koala.Services;
using koala.Data.ViewModels;
using koala.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace koala.Controllers
{
    [ApiController]
    [Route("api/static-pages")]
    public class StaticPagesController : ControllerBase
    {
        private readonly StaticPagesService _staticPagesService;

        public StaticPagesController(StaticPagesService staticPagesService)
        {
            _staticPagesService = staticPagesService;
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPut("history")]
        public async Task<IActionResult> EditHistoryStaticPageMarkdown([FromBody] StaticPageUpdateVM vm)
        {
            var data = await _staticPagesService.UpdateHistory(vm.MarkdownBody);
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryStaticPageMarkdown()
        {
            var data = await _staticPagesService.GetHistory();
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPut("rules")]
        public async Task<IActionResult> EditRulesStaticPageMarkdown([FromBody] StaticPageUpdateVM vm)
        {
            var data = await _staticPagesService.UpdateRules(vm.MarkdownBody);
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpGet("rules")]
        public async Task<IActionResult> GetRulesStaticPageMarkdown()
        {
            var data = await _staticPagesService.GetRules();
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpPut("problems")]
        public async Task<IActionResult> EditProblemsStaticPageMarkdown([FromBody] StaticPageUpdateVM vm)
        {
            var data = await _staticPagesService.UpdateProblems(vm.MarkdownBody);
            return data == null ? NotFound() : Ok(data);
        }

        [Authorize(Roles = "ADMIN,EDITOR")]
        [HttpGet("problems")]
        public async Task<IActionResult> GetProblemsStaticPageMarkdown()
        {
            var data = await _staticPagesService.GetProblems();
            return data == null ? NotFound() : Ok(data);
        }
    }
}