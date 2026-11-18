using AutoMapper;
using Cart.Application.Commands;
using Shop.Contracts.Requests.Cart;
using Shop.Contracts.DTOs.Cart;
using CartEntity = Cart.Domain.Entities.Cart;
using CartItemEntity = Cart.Domain.Entities.CartItem;

namespace Cart.Application.Mappings
{
    public class CartMappingProfile : Profile
    {
        public CartMappingProfile()
        {
            // Request -> Command
            CreateMap<CartItemRequest, AddCartItemCommand>()
                .ForMember(dst => dst.CustomerId, opt => opt.Ignore());

            // Domain -> DTO
            CreateMap<CartItemEntity, CartItemDTO>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.ProductNameSnapshot, o => o.MapFrom(s => s.ProductNameSnapshot))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => s.Quantity))
                .ForMember(d => d.UnitPriceSnapshot, o => o.MapFrom(s => s.UnitPriceSnapshot))
                .ForMember(d => d.LineTotalSnapshot, o => o.MapFrom(s => s.LineTotalSnapshot))
                .ForMember(d => d.IsAvailable, o => o.MapFrom(s => s.IsAvailable));

            CreateMap<CartEntity, CartDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.Revision, o => o.MapFrom(s => s.Revision))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
                .ForMember(d => d.EstimatedTotalCost, o => o.MapFrom(s => s.EstimatedTotalCost));
        }
    }
}
