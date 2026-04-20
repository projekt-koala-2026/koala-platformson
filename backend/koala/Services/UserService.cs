
using koala.Data;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace koala.Services
{
    //FIXME: DECIDE ON THE ENDPOINT STYLE (POST, PUT, GET OR DELETE) !!!
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public UserService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<UserVM>> AdminPanelGetUsersInfo()
        {
            using var context = await _factory.CreateDbContextAsync();

            var userVMs = await context.Users
                .Select(user => new UserVM
                {
                    Email = user.Email,
                    Password = null,
                    Roles = context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Value)
                    .ToList()
                })
                .ToListAsync();

            return userVMs;
        }
    }
}