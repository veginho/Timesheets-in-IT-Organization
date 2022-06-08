using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class ProjectsUser
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int UserId { get; set; }

        public virtual Project Project { get; set; }
        public virtual Credential User { get; set; }
    }
}
