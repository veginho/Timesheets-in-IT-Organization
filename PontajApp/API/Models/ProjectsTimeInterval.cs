using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class ProjectsTimeInterval
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public int? ProjectId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public virtual EverySingleDay Day { get; set; }
        public virtual Project DayNavigation { get; set; }
    }
}
