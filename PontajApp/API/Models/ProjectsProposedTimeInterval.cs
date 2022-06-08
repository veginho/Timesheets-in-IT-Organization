using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class ProjectsProposedTimeInterval
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public int ProjectId { get; set; }
        public int NrOfHours { get; set; }
        public int UserIdProposed { get; set; }

        public virtual EverySingleDay Day { get; set; }
        public virtual Project Project { get; set; }
    }
}
