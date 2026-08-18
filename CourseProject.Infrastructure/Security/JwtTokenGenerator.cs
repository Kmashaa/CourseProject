using CourseProject.Application.Interfaces;
using CourseProject.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace CourseProject.Infrastructure.Security
{
    public class JwtTokenGenerator: IJwtTokenGenerator
    {
        private readonly JwtOptions _jwtOptions;

        public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
        {
            _jwtOptions = jwtOptions.Value;
        }

        public string GenerateJwt(Guid userId, string login, Roles role)
        {
            // 1. Claims
            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.UniqueName] = login,
                ["role"] = role,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            };

            // 2. Ключ и алгоритм подписи
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtOptions.Key!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Описание токена
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Claims = claims,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
                IssuedAt = DateTime.UtcNow,
                SigningCredentials = creds
            };

            // 4. Генерация строки токена
            var tokenString = new JsonWebTokenHandler().CreateToken(descriptor);

            return tokenString;
        }
    }
}
