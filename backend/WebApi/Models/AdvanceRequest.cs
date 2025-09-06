using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class AdvanceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [Required]
        public int ContratoId { get; set; }    
        public Contrato Contrato { get; set; } = null!;

        [Required]
        public AdvanceRequestStatus Status { get; set; } = AdvanceRequestStatus.PENDENTE;

        public string? Notes { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        public List<AdvanceRequestItem> Items { get; set; } = new();
    }
}
