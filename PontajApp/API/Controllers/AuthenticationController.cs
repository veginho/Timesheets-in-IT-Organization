using API.Data;
using API.Models;
using API.TokenMng;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private PontajDbContext _db;
        private readonly IJwtAuthenticationManager jwtAuthenticationManager;

        public AuthenticationController(PontajDbContext db, IJwtAuthenticationManager jwtAuthenticationManager)
        {
            this._db = db;
            this.jwtAuthenticationManager = jwtAuthenticationManager;
        }

        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _db.Credentials.ToList();

            return new JsonResult(users);
        }


        [HttpPost("signup")]
        public IActionResult SignUp([FromBody] AuthenticationMessage message)
        {
            if (message.username == null || message.password == null)
                return BadRequest();

            var user = _db.Credentials.Where(a => a.Username == message.username).FirstOrDefault();
            if (user != null)
                return new JsonResult("User already exists!");

            Credential newCredentials = new Credential();

            newCredentials.Username = message.username;
            newCredentials.Password = message.password;

            _db.Credentials.Add(newCredentials);
            _db.SaveChanges();

            Utils.userId = _db.Credentials.Where(a => a.Username == message.username && a.Password == message.password).Select(a => a.UserId).FirstOrDefault();

            return new JsonResult("User added successfully");
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult LogIn([FromBody] AuthenticationMessage message)
        {
            var token = jwtAuthenticationManager.Authenticate(message.username, message.password, _db);
            if (token == null)
                return Unauthorized();

            return Ok(new
            {
                StatusCode = 200,
                Message = "Logged in successfully!",
                UserData = token
            }) ;
        }

        public IActionResult LogOut()
        {

            return null;
        }
    }
}

