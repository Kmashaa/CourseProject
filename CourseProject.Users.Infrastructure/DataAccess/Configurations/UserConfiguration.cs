using CourseProject.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Users.Infrastructure.DataAccess.Configurations
{
    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(e => e.Login)
                .HasColumnName("login")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(256);

            builder.Property(e => e.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.HasIndex(u => u.Login)
                .IsUnique()
                .HasDatabaseName("IX_Users_Login_Unique");
        }
    }
}
