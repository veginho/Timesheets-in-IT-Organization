using System;
using System.Collections.Generic;

#nullable disable

namespace API.Models
{
    public partial class PlataCuOra
    {
        public int Id { get; set; }
        public int DayId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string SubjectName { get; set; }
        public bool Type { get; set; }
        public string AppForOnline { get; set; }
        public bool StudyProgram { get; set; }
        public string StudyGroup { get; set; }

        public virtual EverySingleDay Day { get; set; }
    }
}
