using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Infrastructure.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string? Key { get; init; }
        public string? Issuer { get; init; }
        public string? Audience { get; init; }
        public int ExpirationMinutes { get; init; }
    }

}
