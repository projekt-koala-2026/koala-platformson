
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
    public class AuthServices
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AuthServices(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task AdminPanelAddUser(UserVM user)
        {

            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY AND ROLES ADDING
            var context = await _factory.CreateDbContextAsync();
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = user.email,
                Password = user.password
            };
            var newToken = new Token
            {
                UserId = newUser.Id,
                Value = null,
                CreatedAt = null,
                LastsFor = null
            };

            context.Users.Add(newUser);
            context.Tokens.Add(newToken);
            await context.SaveChangesAsync();
            return;
        }

        public async Task<string> AdminPanelLogin(UserVM user)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var userDB = context.Users.FirstOrDefault(u => u.Email == user.email && u.Password == user.password);
            if(userDB == null)
            {
                return "";
            }

            var token = await context.Tokens.FirstOrDefaultAsync(t => t.UserId == userDB.Id);

            if (token == null)
            {
                token = new Token
                {
                    UserId = userDB.Id
                };

                context.Tokens.Add(token);
            }

            // update token
            token.Value = Guid.NewGuid().ToString();
            token.CreatedAt = DateTime.UtcNow;
            token.LastsFor = DateTime.UtcNow.AddHours(1);

            await context.SaveChangesAsync();
            return token.Value;
        }

        public async Task AdminPanelLogout(string tokenValue)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var token = await context.Tokens.FirstOrDefaultAsync(t => t.Value == tokenValue);

            if (token == null)
            {
                return; //invalid token
            }

            // update token
            token.Value = null;
            token.CreatedAt = null;
            token.LastsFor = null;

            await context.SaveChangesAsync();
            return;
        }

        public async Task<List<UserVM>> List()
        {
            using var context = await _factory.CreateDbContextAsync();

            var userVMs = await context.Users
                .Select(user => new UserVM
                {
                    email = user.Email,
                    password = user.Password
                })
                .ToListAsync();

            return userVMs;
        }
    }
}
