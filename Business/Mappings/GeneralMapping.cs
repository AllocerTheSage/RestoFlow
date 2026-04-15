using AutoMapper;
using Business.DTOs.ProductDtos;
using Core.Concretes.Entities;

namespace Business.Mappings
{
    public class GeneralMapping : Profile
    {
        public GeneralMapping()
        {
            // Product -> ProductDto dönüşümü (ve tam tersi) için izin veriyoruz.
            // ReverseMap() sayesinde hem Entity'den DTO'ya hem DTO'dan Entity'ye dönüşebilir.
            CreateMap<Product, ProductDto>().ReverseMap();

            // İleride diğer DTO'larımızı da buraya ekleyeceğiz.
        }
    }
}