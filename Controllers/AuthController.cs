using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _repo;
        public AuthController(IAuthRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDto userDto)
        {

            userDto.Username = userDto.Username.ToLower();

            if (await _repo.UserExists(userDto.Username)) {
                ModelState.AddModelError("Username", "Username already exists");
            }

            //validate request
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var userToCreate = new User
            {
                Username = userDto.Username
            };

            var createUser = await _repo.Register(userToCreate, userDto.Password);

            return StatusCode(201);
        }
    }
}