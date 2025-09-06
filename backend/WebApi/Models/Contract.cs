using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class Contrato
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        public string NomeContrato { get; set; } = string.Empty;

        [Required]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.PENDENTE;

        [Required]
        public DateTime VencimentoContrato { get; set; }

        [Required]
        public DateTime DataAlteracao { get; set; }

        [Required]
        public DateTime DataInsercao { get; set; }

        [Required]
        public int NumeroParcelas { get; set; }

        public List<Parcela> Parcelas { get; set; } = new();
    }
}
