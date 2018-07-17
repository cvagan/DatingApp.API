using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _db;
        public DatingRepository(DataContext db)
        {
            _db = db;
        }
        public void Add<T>(T entity) where T : class
        {
            _db.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _db.Remove(entity);
        }

        // returns a user found in the database based on the provided id
        public async Task<User> GetUser(int id)
        {
            var user = await _db.Users.Include(p => p.Photos).FirstOrDefaultAsync(u => u.Id == id);

            return user;
        }

        // returns all users in the database
        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await _db.Users.Include(p => p.Photos).ToListAsync();

            return users;
        }

        // saves all changes to the database
        public async Task<bool> SaveAll()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}