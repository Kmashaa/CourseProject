using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Domain.Entities
{
    public class User
    {
        public required Guid Id { get; init; }

        public required string Login { get; init; }

        public required string PasswordHash { get; init; }

        public required Roles Role { get; set; }

    }

    public enum Roles
    {
        User = 1,
        Admin = 2
    }
}
