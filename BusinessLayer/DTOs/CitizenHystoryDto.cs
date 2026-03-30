using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs
{
    public class CitizenHistoryDto
    {
        public Guid Id { get; set; }
        public string DocumentName { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string RejectionReason { get; set; }
    }
}
