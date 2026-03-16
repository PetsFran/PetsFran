using System.Collections.Generic;

namespace PetsFran.API.Services.Data
{
    public static class ServicosAdicionaisData
    {
        public static Dictionary<string, Dictionary<string, decimal>> GetAdicionais()
        {
            return new Dictionary<string, Dictionary<string, decimal>>
            {
                ["Escovação de Dente"] = new Dictionary<string, decimal> { ["P"] = 15, ["M"] = 15, ["G"] = 15 },
                ["Desembolo"] = new Dictionary<string, decimal> { ["P"] = 15, ["M"] = 25, ["G"] = 35 },
                ["Hidratação"] = new Dictionary<string, decimal> { ["P"] = 20, ["M"] = 20, ["G"] = 20 },
                ["Cortar Unha"] = new Dictionary<string, decimal> { ["P"] = 10, ["M"] = 10, ["G"] = 10 }
            };
        }
    }
}