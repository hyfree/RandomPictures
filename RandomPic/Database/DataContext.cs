using Microsoft.EntityFrameworkCore;

using MoreNote.Logic.Entity;

using System;
using System.Collections.Generic;

namespace MoreNote.Logic.Database
{
    public class DataContext : DbContext
    {
        //public DataContext()
        //{
        //}
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    base.OnConfiguring(optionsBuilder);
        //    //测试服务器
        //    ConfigFileService configFileService = new ConfigFileService();
        //    var postgres = configFileService.WebConfig;
        //    optionsBuilder.UseNpgsql(postgres.PostgreSql.Connection);

        //}
        public DataContext(DbContextOptions<DataContext> options)
          : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //建立索引

        }

       
       

        //随机图片服务
        public DbSet<RandomImage> RandomImage { get; set; }

    
    }
}