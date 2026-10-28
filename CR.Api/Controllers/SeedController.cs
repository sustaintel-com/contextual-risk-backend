using CR.Service;
using Microsoft.AspNetCore.Mvc;
 
namespace CR.Api.Controllers;
 
[ApiController]
[Route("[controller]")]
public class SeedController(Seeder seeder) : ControllerBase
{
    private readonly Seeder _seeder = seeder;
 
    [HttpPost]
    public async Task<IActionResult> SeedDatabase()
    {
        await _seeder.SeedDatabaseAsync();
        return Ok("Database seeding completed successfully.");
    }
}