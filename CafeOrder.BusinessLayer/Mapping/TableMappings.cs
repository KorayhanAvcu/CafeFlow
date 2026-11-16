using AutoMapper;
using CafeOrder.DtoLayer.Dtos.TableDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Masa eşlemeleri (AutoMapper Profile).</summary>
public class TableMappings : Profile
{
    public TableMappings()
    {
        CreateMap<CreateTableDto, Table>();
        CreateMap<UpdateTableDto, Table>().ReverseMap();   // QrToken DTO'da yok → düzenlemede korunur
        CreateMap<UpdateTableDto, CreateTableDto>();

        // Sipariş sayısı Orders listesinden sayılır.
        CreateMap<Table, ResultTableDto>()
            .ForMember(dest => dest.OrderCount, opt => opt.MapFrom(src => src.Orders.Count));
    }
}
