using System;
using System.Collections.Generic;
using System.Linq;
using PetsFran.API.Models;
using PetsFran.API.Models.Entities;
using PetsFran.API.Models.DTOs.Precos;
using PetsFran.API.Services.Data;
using PetsFran.API.Data;

namespace PetsFran.API.Services
{
    public class CalculaPrecosService
    {
        private readonly List<ServicoBase> _servicosBase;
        private readonly Dictionary<string, Dictionary<string, decimal>> _adicionais;
        private readonly AppDbContext _context;

        public CalculaPrecosService(AppDbContext context)
        {
            _context = context;
            _servicosBase = ServicosBaseData.GetServicosBase();
            _adicionais = ServicosAdicionaisData.GetAdicionais();
        }

        // Métodos auxiliares para comparação de strings
        private bool StringsIguais(string? a, string? b)
        {
            if (a == null || b == null) return false;
            return a.Trim().Equals(b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public decimal GetPrecoBasePorCachorro(Cachorro pet, List<string> servicosBase, int petShopId = 0)
        {
            var item = _servicosBase.Find(s => 
                StringsIguais(s.Porte, pet.Porte) && 
                StringsIguais(s.Raca, pet.Raca));
            
            if (item == null || servicosBase.Any(nome => !item.Servicos.ContainsKey(nome)))
                throw new ArgumentException("Serviço base não disponível para a raça e porte informados");

            return servicosBase.Sum(nome => PrecoAtual(petShopId, "Base", item.Porte, item.Raca,
                nome, item.Servicos[nome]));
        }

        public decimal GetPrecoAdicionalPorCachorro(Cachorro pet, List<string> adicionais, int petShopId = 0)
        {
            var adicionalInvalido = adicionais.FirstOrDefault(nome => !_adicionais.ContainsKey(nome));
            if (adicionalInvalido != null)
                throw new ArgumentException($"Serviço adicional não disponível: {adicionalInvalido}");

            var porteKey = (pet.Porte?.Trim() ?? "").ToUpper() switch
            {
                "PEQUENO" => "P",
                "MÉDIO" => "M",
                "MEDIO" => "M",
                "GRANDE" => "G",
                _ => throw new ArgumentException("Porte do pet não disponível na tabela de preços")
            };

            return adicionais.Sum(nome => PrecoAtual(petShopId, "Adicional", porteKey, "",
                nome, _adicionais[nome][porteKey]));
        }

        public decimal GetPrecoTotalHorario(Horario horario)
        {
            if (horario.Cachorro == null) return 0;
    
            return GetPrecoBasePorCachorro(horario.Cachorro,
                       new List<string> { horario.ServicoBaseSelecionado }, horario.PetShopId) +
                   GetPrecoAdicionalPorCachorro(horario.Cachorro, horario.Adicionais, horario.PetShopId);
        }

        private decimal PrecoAtual(int petShopId, string tipo, string porte, string raca,
            string nome, decimal precoPadrao)
        {
            if (petShopId <= 0) return precoPadrao;
            return _context.PrecosServicos
                .Where(p => p.PetShopId == petShopId && p.Tipo == tipo && p.Porte == porte &&
                            p.Raca == raca && p.Nome == nome)
                .Select(p => (decimal?)p.Valor).FirstOrDefault() ?? precoPadrao;
        }

        public List<PrecoServicoCatalogo> GetCatalogo(int petShopId)
        {
            var alteracoes = _context.PrecosServicos.Where(p => p.PetShopId == petShopId).ToList();
            var baseItens = _servicosBase.SelectMany(item => item.Servicos.Select(servico =>
                CriarItem("Base", item.Porte, item.Raca, servico.Key, servico.Value, alteracoes)));
            var adicionais = _adicionais.SelectMany(item => item.Value.Select(porte =>
                CriarItem("Adicional", porte.Key, "", item.Key, porte.Value, alteracoes)));
            return baseItens.Concat(adicionais).ToList();
        }

        public PrecoServicoCatalogo? GetItemCatalogo(string tipo, string porte, string raca, string nome)
        {
            if (tipo == "Base")
            {
                var item = _servicosBase.FirstOrDefault(s => StringsIguais(s.Porte, porte) && StringsIguais(s.Raca, raca));
                return item != null && item.Servicos.TryGetValue(nome, out var valor)
                    ? new PrecoServicoCatalogo("Base", item.Porte, item.Raca, nome, valor)
                    : null;
            }
            if (tipo == "Adicional" && _adicionais.TryGetValue(nome, out var portes) &&
                portes.TryGetValue(porte.ToUpperInvariant(), out var preco))
                return new PrecoServicoCatalogo("Adicional", porte.ToUpperInvariant(), "", nome, preco);
            return null;
        }

        private static PrecoServicoCatalogo CriarItem(string tipo, string porte, string raca,
            string nome, decimal padrao, List<PrecoServico> alteracoes)
        {
            var alterado = alteracoes.FirstOrDefault(p => p.Tipo == tipo && p.Porte == porte &&
                p.Raca == raca && p.Nome == nome);
            return new PrecoServicoCatalogo(tipo, porte, raca, nome, alterado?.Valor ?? padrao);
        }

        public List<string> GetRacasPorPorte(string? porte)
        {
            if (string.IsNullOrWhiteSpace(porte))
                return new List<string>();
                
            return _servicosBase
                .Where(item => StringsIguais(item.Porte, porte))
                .Select(item => item.Raca)
                .Distinct()
                .ToList();
        }

        public List<string> GetServicosPorRacaPorte(string? raca, string? porte)
        {
            if (string.IsNullOrWhiteSpace(raca) || string.IsNullOrWhiteSpace(porte))
                return new List<string>();
            
            var item = _servicosBase.FirstOrDefault(s => 
                StringsIguais(s.Porte, porte) && 
                StringsIguais(s.Raca, raca));
            
            return item?.Servicos.Keys.ToList() ?? new List<string>();
        }

        public List<string> GetServicosAdicionais()
        {
            return _adicionais.Keys.ToList();
        }

        // MÉTODO ADICIONAL PARA DEBUG
        public Dictionary<string, object> DebugBuscaServicos(string raca, string porte)
        {
            var resultado = new Dictionary<string, object>();
            
            resultado["RacaInformada"] = raca;
            resultado["PorteInformado"] = porte;
            resultado["RacaTrimmed"] = raca?.Trim();
            resultado["PorteTrimmed"] = porte?.Trim();
            
            // Todas as combinações disponíveis
            var todasCombinacoes = _servicosBase
                .Select(s => new 
                { 
                    PorteOriginal = s.Porte, 
                    PorteTrimmed = s.Porte?.Trim(),
                    RacaOriginal = s.Raca, 
                    RacaTrimmed = s.Raca?.Trim(),
                    Servicos = s.Servicos.Keys.ToList()
                })
                .ToList();
            
            resultado["TodasCombinacoes"] = todasCombinacoes;
            
            // Busca específica
            var itemEncontrado = _servicosBase.FirstOrDefault(s => 
                StringsIguais(s.Porte, porte) && 
                StringsIguais(s.Raca, raca));
            
            resultado["ItemEncontrado"] = itemEncontrado != null;
            resultado["ServicosDisponiveis"] = itemEncontrado?.Servicos.Keys.ToList() ?? new List<string>();
            
            return resultado;
        }
    }
}
