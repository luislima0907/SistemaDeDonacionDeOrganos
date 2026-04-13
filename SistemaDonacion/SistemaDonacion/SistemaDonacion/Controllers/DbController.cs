using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class DbController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DbController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dbcheck")]
        public async Task<IActionResult> DbCheck()
        {
            var result = new
            {
                Connected = false,
                DataSource = string.Empty,
                Database = string.Empty,
                HasAspNetRoles = false,
                HasAspNetUsers = false,
                Message = string.Empty
            };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var ds = connection.DataSource;
                var db = connection.Database;

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT OBJECT_ID('dbo.AspNetRoles')";
                var r1 = await cmd.ExecuteScalarAsync();

                cmd.CommandText = "SELECT OBJECT_ID('dbo.AspNetUsers')";
                var r2 = await cmd.ExecuteScalarAsync();

                return Ok(new
                {
                    Connected = true,
                    DataSource = ds,
                    Database = db,
                    HasAspNetRoles = (r1 != null && r1 != DBNull.Value),
                    HasAspNetUsers = (r2 != null && r2 != DBNull.Value),
                    Message = "OK"
                });
            }
            catch (DbException dbex)
            {
                return StatusCode(500, new { Connected = false, Message = dbex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Connected = false, Message = ex.Message });
            }
        }
    }
}
