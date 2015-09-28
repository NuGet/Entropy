namespace WebApplication1.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Collections.Generic;

    public partial class AdminModel : DbContext
    {
        public AdminModel()
            : base("name=IssueModel")
        {
        }

        public virtual DbSet<Admin> Admins { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>()
                .Property(e => e.UserName)
                .IsUnicode(false);
        }

        public List<Admin> GetAllAdmins()
        {
            var names = from a in Admins
                        select a;

            return names.ToList();
        }

        public List<String> GetAllUserNames()
        {
            var names = from a in Admins
                        select (a.UserName);

            return names.ToList();
        }

        public string GetUserNameById(int id)
        {
            var name = from a in Admins
                       where a.Id == id
                       select (a.UserName);

            if (name.Count() > 0)
            {
                return name.First();
            }
            else
            {
                return null;
            }
        }

        public int GetIdByUserName(String userName)
        {
            if (String.IsNullOrEmpty(userName))
                return -1;

            var id = from a in Admins
                       where a.UserName == userName
                       select (a.Id);

            if (id.Count() > 0)
            {
                return id.First();
            }
            else
            {
                return -1;
            }
        }
    }
}
