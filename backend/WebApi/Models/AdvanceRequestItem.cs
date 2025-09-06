using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class AdvanceRequestItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AdvanceRequestId { get; set; }
        public AdvanceRequest AdvanceRequest { get; set; } = null!;

        [Required]
        public int ParcelaId { get; set; }
        public Parcela Parcela { get; set; } = null!;

        public decimal? ValorNaSolicitacao { get; set; }
    }
}
