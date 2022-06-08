using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class Project
    {
        public Project()
        {
            ProjectsProposedTimeIntervals = new HashSet<ProjectsProposedTimeInterval>();
            ProjectsTimeIntervals = new HashSet<ProjectsTimeInterval>();
            ProjectsUsers = new HashSet<ProjectsUser>();
        }

        public int ProjectId { get; set; }
        public int DirectorId { get; set; }
        public string ProjectName { get; set; }
        public bool Status { get; set; }

        public virtual Credential Director { get; set; }
        public virtual ICollection<ProjectsProposedTimeInterval> ProjectsProposedTimeIntervals { get; set; }
        public virtual ICollection<ProjectsTimeInterval> ProjectsTimeIntervals { get; set; }
        public virtual ICollection<ProjectsUser> ProjectsUsers { get; set; }
    }
}
