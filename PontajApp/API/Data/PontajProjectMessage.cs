using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Data
{
    public class PontajProjectMessage : PontajMessage
    {
        public string startTime { get; set; } = null;
        public string endTime { get; set; } = null;
        public string projectName { get; set; } = null;
    }
}
