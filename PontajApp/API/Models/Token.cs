using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class Token
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token1 { get; set; }

        public virtual Credential User { get; set; }
    }
}
