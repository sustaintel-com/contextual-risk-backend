
using CR.Model.Data;

namespace CR.Service
{
    public class Seeder
    {
        private readonly CRContext _crContext;
 
        public Seeder(CRContext crContext)
        {
            _crContext = crContext;
        }
 
        public async Task SeedDatabaseAsync()
        {
            await _crContext.SeedDataAsync();
        }
    }
}