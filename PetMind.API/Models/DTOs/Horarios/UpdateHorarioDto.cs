using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PetMind.API.Models.DTOs.Horarios
{
    public class UpdateHorarioDto
    {
        public string Data { get; set; }

        public string ServicoBaseSelecionado { get; set; }

        public List<string> Adicionais { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "ID do petshop inválido")]
        public int? PetShopId { get; set; }
    }
}