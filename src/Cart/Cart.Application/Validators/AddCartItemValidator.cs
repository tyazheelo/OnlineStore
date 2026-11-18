using FluentValidation;
using Cart.Application.Commands;

namespace Cart.Application.Validators
{
    public class AddCartItemValidator : AbstractValidator<AddCartItemCommand>
    {
        public AddCartItemValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required");
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity should be positive");
        }
    }
}
