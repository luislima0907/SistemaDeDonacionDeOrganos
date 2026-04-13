using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.Services;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHashService _passwordHashService;

        public UsersController(AppDbContext dbContext, IPasswordHashService passwordHashService)
        {
            _dbContext = dbContext;
            _passwordHashService = passwordHashService;
        }
    }
}

