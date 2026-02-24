using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManagerWPF.Models
{
    public class ThreadInfo
    {
        public int Id { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public TimeSpan ProcessorTime { get; set; }
    }
}