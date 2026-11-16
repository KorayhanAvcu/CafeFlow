using AutoMapper;
using CafeOrder.DtoLayer.Dtos.ProductDtos;
using CafeOrder.DtoLayer.Dtos.ProductOptionDtos;
using CafeOrder.EntityLayer.Entities;

namespace CafeOrder.BusinessLayer.Mapping;

/// <summary>Ürün ve ürün seçeneği eşlemeleri (AutoMapper Profile).</summary>
public class ProductMappings : Profile
{
    public ProductMappings()
    {
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>().ReverseMap();
        CreateMap<UpdateProductDto, CreateProductDto>();

        // CategoryName, CategorySlug, CategoryIcon: AutoMapper bunları Category.Name, Category.Slug, Category.Icon'dan
        // kendisi doldurur ("flattening": hedef adını Category + Name diye bölüp kaynakta arar). Kural yazmaya gerek yok.
        CreateMap<Product, ResultProductDto>();

        CreateMap<ProductOption, ResultProductOptionDto>();
    }
}
