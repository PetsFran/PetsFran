using System;
using AutoMapper;
using PetsFran.API.Models.DTOs.Cachorros;
using PetsFran.API.Models.DTOs.Horarios;
using PetsFran.API.Models.DTOs.PetShops;
using PetsFran.API.Models.Entities;
using System.Globalization;

namespace PetsFran.API.Profiles
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Cachorro
            CreateMap<CreateCachorroDto, Cachorro>();
            CreateMap<UpdateCachorroDto, Cachorro>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Cachorro, CachorroResponseDto>();

            // Horario
            CreateMap<CreateHorarioDto, Horario>();
            CreateMap<UpdateHorarioDto, Horario>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            
            CreateMap<Horario, HorarioResponseDto>()
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => 
                    ConverterParaHorarioBrasilia(src.Data)))
                .ForMember(dest => dest.ValorTotal, opt => opt.MapFrom(src => src.ValorTotal))
                .ForMember(dest => dest.Cachorro, opt => opt.MapFrom(src => src.Cachorro))
                .ForMember(dest => dest.PetShop, opt => opt.MapFrom(src => src.PetShop));

            // PetShop
            CreateMap<CreatePetShopDto, PetShop>();
            CreateMap<UpdatePetShopDto, PetShop>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            
            CreateMap<PetShop, PetShopBasicDto>();
            
            CreateMap<PetShop, PetShopResponseDto>()
                .ForMember(dest => dest.Horarios, opt => opt.MapFrom(src => src.Horarios));

            // Mapeamentos auxiliares
            CreateMap<Cachorro, CachorroInfoDto>()
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(src => src.NomeCachorro))
                .ForMember(dest => dest.NomeTutor, opt => opt.MapFrom(src => src.NomeTutor))
                .ForMember(dest => dest.ContatoTutor, opt => opt.MapFrom(src => src.ContatoTutor))
                .ForMember(dest => dest.EnderecoCachorro, opt => opt.MapFrom(src => src.EnderecoCachorro));

            CreateMap<PetShop, PetShopInfoDto>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EnderecoPetShop, opt => opt.MapFrom(src => src.EnderecoPetShop));
        }

        // Método auxiliar para converter UTC para Horário de Brasília
        private static string ConverterParaHorarioBrasilia(DateTime dataUtc)
        {
            try
            {
                // Define o fuso horário de Brasília
                TimeZoneInfo brasiliaTimeZone = 
                    TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                
                // Converte de UTC para Brasília
                DateTime dataBrasilia = TimeZoneInfo.ConvertTimeFromUtc(dataUtc, brasiliaTimeZone);
                
                // Formata no padrão brasileiro
                return dataBrasilia.ToString("dd/MM/yyyy HH:mm:ss", 
                    CultureInfo.GetCultureInfo("pt-BR"));
            }
            catch (Exception)
            {
                // Fallback: se algo der errado, retorna a data original formatada
                return dataUtc.ToString("dd/MM/yyyy HH:mm:ss", 
                    CultureInfo.GetCultureInfo("pt-BR"));
            }
        }
    }
}