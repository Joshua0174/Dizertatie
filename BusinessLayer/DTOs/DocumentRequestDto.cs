using System;

namespace BusinessLayer.DTOs
{
    public class DocumentRequestDto
    {
        // ID-ul cererii (ca să știm la ce s-a răspuns)
        public Guid Id { get; set; }

        // ID-ul funcționarului (Avem nevoie de el strict în Controller pentru SignalR)
        public string OfficialId { get; set; }

        // Statusul transformat direct în String ("Approved" sau "Rejected")
        public string Status { get; set; }
    }
}