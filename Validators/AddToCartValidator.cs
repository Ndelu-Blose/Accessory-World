using AccessoryWorld.DTOs;
using FluentValidation;

namespace AccessoryWorld.Validators
{
    public class AddToCartValidator : AbstractValidator<AddToCartDto>
    {
        public AddToCartValidator()
        {
            RuleFor(x => x.SkuCode)
                .NotEmpty().WithMessage("SKU code is required")
                .MaximumLength(50).WithMessage("SKU code cannot exceed 50 characters")
                .Matches(@"^[A-Z0-9-]+$").WithMessage("SKU code can only contain uppercase letters, numbers, and hyphens");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 items");
        }
    }
}