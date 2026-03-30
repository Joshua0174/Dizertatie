using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class StatMetrics
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Resolved { get; set; }
        public int Expired { get; set; }
    }
}
