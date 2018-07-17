using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        // constructor takes in configuration data and the authentication repository
        public AuthController(IAuthRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _config = configuration;
        }

        // api/auth/register post route
        // returns 201 created or BadRequest if username and password requirements are not met
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserForRegisterDto userDto)
        {
            if (!string.IsNullOrEmpty(userDto.Username)) {
                userDto.Username = userDto.Username.ToLower();
            }

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

        // api/auth/login post route
        // takes json object from request body and returns Ok with tokenstring or Unauthorized
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody]UserForLoginDto userDto)
        {
            var userFromRepo = await _repo.Login(userDto.Username.ToLower(), userDto.Password);

            if (userFromRepo == null) {
                return Unauthorized();
            }

            //generate token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config.GetSection("AppSettings:Token").Value);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                    new Claim(ClaimTypes.Name, userFromRepo.Username)
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new {tokenString});
        }
    }
}