using koala.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.AspNetCore.Http.HttpResults;
using koala.Data.ViewModels;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

// namespace koala.Controllers
// {
//     //FIXME: make sure params are passed corectly (VALIDATION!!!!)
//     [ApiController]
//     [Route("api/admin/user")]
//     public class AdminUserController : ControllerBase
//     {
//         public UserServices _userServices;
//         public ValidationService _validationService;

//         public AdminAuthController(AuthServices authServices, ValidationService validationService)
//         {
//             _authServices = authServices;
//             _validationService = validationService;
//         }
//     }
// }