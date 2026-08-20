using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CourseProject.Domain.Entities
{
    public class User
    {

        [SetsRequiredMembers]
        public User(Guid id, string login, string passwordHash, Roles role)
        {
            Id = id;
            Login = login;
            PasswordHash = passwordHash;
            Role = role;
        }

        public required Guid Id { get; init; }

        public required string Login { get; init; }

        public required string PasswordHash { get; init; }

        public required Roles Role { get; set; }

        public ICollection<Booking> Bookings { get; set; } = [];


        public static User Create(
            string Login,
            string PasswordHash,
            Roles Role)
        {
            return new User(Guid.NewGuid(), Login, PasswordHash, Role);
        }

    }

    public enum Roles
    {
        User = 1,
        Admin = 2
    }


}
