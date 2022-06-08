using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class EverySingleDay
    {
        public EverySingleDay()
        {
            FisaPostuluiDeBazas = new HashSet<FisaPostuluiDeBaza>();
            PlataCuOras = new HashSet<PlataCuOra>();
            ProjectsProposedTimeIntervals = new HashSet<ProjectsProposedTimeInterval>();
            ProjectsTimeIntervals = new HashSet<ProjectsTimeInterval>();
        }

        public int DayId { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }

        public virtual Credential User { get; set; }
        public virtual ICollection<FisaPostuluiDeBaza> FisaPostuluiDeBazas { get; set; }
        public virtual ICollection<PlataCuOra> PlataCuOras { get; set; }
        public virtual ICollection<ProjectsProposedTimeInterval> ProjectsProposedTimeIntervals { get; set; }
        public virtual ICollection<ProjectsTimeInterval> ProjectsTimeIntervals { get; set; }
    }
}
