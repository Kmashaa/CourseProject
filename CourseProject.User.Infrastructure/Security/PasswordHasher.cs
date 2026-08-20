using CourseProject.Users.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CourseProject.Users.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        public bool Verify(string password, string passwordHash)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes) == passwordHash;
        }
    }
}
