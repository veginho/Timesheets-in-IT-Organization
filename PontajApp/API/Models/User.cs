using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class User
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mail { get; set; }

        public virtual Credential UserNavigation { get; set; }
    }
}
