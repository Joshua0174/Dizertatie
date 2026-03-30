using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class OfficialStatsDto
    {
        public StatMetrics AllTime { get; set; } = new StatMetrics();
        public StatMetrics Today { get; set; } = new StatMetrics();
    }
}
