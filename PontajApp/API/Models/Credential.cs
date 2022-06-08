using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class Credential
    {
        public Credential()
        {
            EverySingleDays = new HashSet<EverySingleDay>();
            Projects = new HashSet<Project>();
            ProjectsUsers = new HashSet<ProjectsUser>();
            Tokens = new HashSet<Token>();
            Users = new HashSet<User>();
        }

        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public virtual ICollection<EverySingleDay> EverySingleDays { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
        public virtual ICollection<ProjectsUser> ProjectsUsers { get; set; }
        public virtual ICollection<Token> Tokens { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
