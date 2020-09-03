using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text;

namespace DynamicDataStore.Core.Db
{
    public class DbContextBase : DbContext
    {
        public DbContextBase()
        {

        }

        public DbContextBase(string connectionString) : base(GetOptions(connectionString))
        {

        }

        public DbContextBase(DbContextOptions<DbContextBase> options)
            : base(options)
        {

        }

        private static DbContextOptions GetOptions(string connectionString)
        {
            return new DbContextOptionsBuilder().UseSqlServer(connectionString).Options;
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public void CancelChanges()
        {
            foreach (EntityEntry entry in this.ChangeTracker.Entries())
            {
                if (entry.CurrentValues != entry.OriginalValues)
                {
                    entry.Reload();
                }
            }
        }

        public void CancelChanges<T>() where T : class
        {
            foreach (EntityEntry<T> entry in this.ChangeTracker.Entries<T>())
            {
                if (entry.CurrentValues != entry.OriginalValues)
                {
                    entry.Reload();
                }
            }
        }

        public override int SaveChanges()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                return base.SaveChanges();
            }
            //catch (EntityValidationException e)
            //{
            //    foreach (var eve in e.EntityValidationErrors)
            //    {
            //        sb.AppendLine(string.Format("Type \"{0}\" in state \"{1}\" has these validation errors:", eve.Entry.Entity.GetType().Name, eve.Entry.State));
            //        foreach (var ve in eve.ValidationErrors)
            //        {
            //            sb.AppendLine(string.Format("Property Name: \"{0}\", Error Message: \"{1}\"",
            //                ve.PropertyName, ve.ErrorMessage));
            //        }
            //    }

            //    throw new System.Exception(sb.ToString(), e);
            //}
            catch (System.Exception exp)
            {
                throw new System.Exception("Error ContextBase.SaveChanges Method", exp);
            }
        }
    }
}
