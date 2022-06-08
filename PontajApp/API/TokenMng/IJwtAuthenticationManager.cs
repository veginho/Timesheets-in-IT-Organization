using API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.TokenMng
{
    public interface IJwtAuthenticationManager
    {
        string Authenticate(string username, string password, PontajDbContext _db);
        int ValidateToken(string token, PontajDbContext db);
    }
}
