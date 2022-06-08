using API.Models;
using API.TokenMng;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected PontajDbContext db;
        protected readonly IJwtAuthenticationManager jwtAuthenticationManager;
        protected int userID;

        public BaseApiController(PontajDbContext db, IJwtAuthenticationManager jwtAuthenticationManager, IHttpContextAccessor httpContextAccessor)
        {
            this.jwtAuthenticationManager = jwtAuthenticationManager;
            this.db = db;

            string token = null;
            httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Authorization", out var output);
            token = output.ToString();
            token = token.Substring(7);
            userID = jwtAuthenticationManager.ValidateToken(token, db);
        }


    }
}
