using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _db;
        public AuthRepository(DataContext db)
        {
            _db = db;
        }

        // returns user object if provided username and password is verified
        public async Task<User> Login(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Username == username);

            if (user == null) 
            {
                return null;
            }

            if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            return user;
        }

        // helper method for verifying password hash
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        // hashes password, stores user in database and returns user
        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            return user;
        }

        // helper method for creating password hash
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        // method to check if user exists in database based on provided username
        public async Task<bool> UserExists(string username)
        {
            var foundUser = await _db.Users.FirstOrDefaultAsync(user => user.Username == username);

            return foundUser != null;
        }
    }
}