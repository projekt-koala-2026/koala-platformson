
using koala.Data;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;

namespace koala.Services
{
    public class ValidationService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ValidationService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // public ValidateEndpoint()
        // {

        // }

    }
}