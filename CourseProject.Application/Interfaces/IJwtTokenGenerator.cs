using CourseProject.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwt(Guid userId, string login, Roles role);
    }
}
