using ASC.Model.Models;
using ASC.Web.Areas.Configuration.Models;
using AutoMapper;

namespace ASC.Web.Areas.Configuration.Models
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MasterDataKey, MasterDataKeyViewModel>()
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.PartitionKey))
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.RowKey))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ReverseMap();

            CreateMap<MasterDataValue, MasterDataValueViewModel>()
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.PartitionKey))
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.RowKey))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ReverseMap();
        }
    }
}