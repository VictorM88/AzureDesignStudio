﻿using AzureDesignStudio.SharedModels.User;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AzureDesignStudio.Server.Models
{
    public class DesignDbContext : DbContext
    {
        public DesignDbContext(DbContextOptions options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DesignModel>()
                .HasCheckConstraint("ADS_CHECK_JSON", "ISJSON(DesignData)=1");
        }

        public DbSet<DesignModel> AdsDesigns { get; set; } = null!;
        public DbSet<AzureSubscriptionModel> AzureSubscriptions { get; set; } = null!;
        public DbSet<UserSettingModel> UserSettings { get; set; } = null!;
    }

    public record DesignModel
    {
        public int Id { get; set; }
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = null!;
        public Guid UserId { get; set; }
        public string DesignData { get; set; } = null!;
        public DateTime LastModified { get; set; }
    }

    public record AzureSubscriptionModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        [StringLength(64, MinimumLength = 1)]
        public string SubscriptionName { get; set; } = null!;
        public Guid TenantId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientSecret { get; set; } = null!;
    }

    public record UserSettingModel
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public UserSettingType Type { get; set; }
        public string Value { get; set; }
    }
}
