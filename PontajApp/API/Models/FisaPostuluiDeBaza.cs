using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class FisaPostuluiDeBaza
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime? RecoveredFrom { get; set; }

        public virtual EverySingleDay Day { get; set; }
    }
}
