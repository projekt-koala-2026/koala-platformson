
using koala.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace koala.Services
{
    public class AuthServices
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AuthServices(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task Register()
        {
            return;
        }

        public async Task Login()
        {
            return;
        }

        public async Task Logout()
        {
            return;
        }
    }
}
