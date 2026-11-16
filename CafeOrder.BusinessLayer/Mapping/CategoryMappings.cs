using AutoMapper;
using CafeOrder.DtoLayer.Dtos.CategoryDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Kategori eşlemeleri (AutoMapper Profile).</summary>
public class CategoryMappings : Profile
{
    public CategoryMappings()
    {
        // Yeni kayıt: formdan gelen DTO → entity
        CreateMap<CreateCategoryDto, Category>();

        // Düzenleme: DTO → var olan entity (_mapper.Map(dto, category)),
        // ReverseMap: entity → form (düzenleme sayfası açılırken)
        CreateMap<UpdateCategoryDto, Category>().ReverseMap();

        // Formun kaydet düğmesi: Id 0 ise UpdateCategoryDto → CreateCategoryDto
        CreateMap<UpdateCategoryDto, CreateCategoryDto>();

        // Listeleme: ürün sayısı entity'de alan olarak yok; Products listesinden sayılır.
        // (Products Include edilmediyse 0 gelir; repository'ler Include yapıyor.)
        CreateMap<Category, ResultCategoryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count));
    }
}
