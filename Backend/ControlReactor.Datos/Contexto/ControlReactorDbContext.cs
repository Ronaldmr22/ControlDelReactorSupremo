using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Datos.Contexto
{
    public class ControlReactorDbContext : DbContext
    {
        public ControlReactorDbContext(DbContextOptions<ControlReactorDbContext> options): base(options)
        {
        }
    }
}