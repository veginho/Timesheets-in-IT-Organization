using API.Data;
using API.Models;
using API.TokenMng;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace API.TokenMng
{
    public class JwtAuthenticationManager : IJwtAuthenticationManager
    {
        private readonly string key;

        public JwtAuthenticationManager(string key)
        {
            this.key = key;
        }

        public string Authenticate(string username, string password, PontajDbContext _db)
        {
            if (username == null || password == null)
                return null;

            Credential credentials = new Credential();
            credentials.Password = password;
            credentials.Username = username;

            var credentialsFound = _db.Credentials.Where(a =>
                                a.Username == credentials.Username && a.Password == credentials.Password).SingleOrDefault();

            if (credentialsFound == null)
                return null;

            

            //Utils.userId = credentialsFound.UserId;

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
               {
                   new Claim(ClaimTypes.Name, username)
               }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials =
                new SigningCredentials(
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string tokenStr = tokenHandler.WriteToken(token);

            Token tk = new Token();
            tk.UserId = credentialsFound.UserId;
            tk.Token1 = SHA256.HashData(Encoding.ASCII.GetBytes(tokenStr)).ToString();
            _db.Tokens.Add(tk);
            _db.SaveChanges();

            return tokenStr;
        }

        public int ValidateToken(string token, PontajDbContext db)
        {
            string hashedTk = SHA256.HashData(Encoding.ASCII.GetBytes(token)).ToString();

            var obj = db.Tokens.Where(a => a.Token1 == hashedTk).FirstOrDefault();

            if (obj == null)
                return -1;

            return obj.UserId;
        }
    }
}
