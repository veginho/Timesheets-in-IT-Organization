using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Data
{
    public class PontajPlataCuOraMessage : PontajMessage
    {
        public string startTime { get; set; } = null;
        public string endTime { get; set; } = null;
        public string subjectName { get; set; } = null;
        public string studyGroup { get; set; } = null;
        public string formatType { get; set; } = null;   // online/fizic
        public string appForOnline { get; set; } = null;

        public string studyProgram { get; set; } = null; // master/licenta
    }
}
