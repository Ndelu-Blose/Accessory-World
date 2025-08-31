using AccessoryWorld.ViewModels;
using FluentValidation;

namespace AccessoryWorld.Validators
{
    public class CheckoutViewModelValidator : AbstractValidator<CheckoutViewModel>
    {
        public CheckoutViewModelValidator()
        {
            RuleFor(x => x.CartItems)
                .NotEmpty().WithMessage("Cart must contain at least one item")
                .Must(items => items.All(item => item.Quantity > 0))
                .WithMessage("All cart items must have a positive quantity");

            RuleFor(x => x.FulfillmentMethod)
                .NotEmpty().WithMessage("Fulfillment method is required")
                .Must(method => method == "delivery" || method == "pickup")
                .WithMessage("Fulfillment method must be either 'delivery' or 'pickup'");

            RuleFor(x => x.SelectedAddressId)
                .NotNull().WithMessage("Delivery address is required")
                .When(x => x.FulfillmentMethod == "delivery");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");

            RuleFor(x => x.SubTotal)
                .GreaterThanOrEqualTo(0).WithMessage("Subtotal must be non-negative");

            RuleFor(x => x.VATAmount)
                .GreaterThanOrEqualTo(0).WithMessage("VAT amount must be non-negative");

            RuleFor(x => x.ShippingFee)
                .GreaterThanOrEqualTo(0).WithMessage("Shipping fee must be non-negative");

            RuleFor(x => x.Total)
                .GreaterThan(0).WithMessage("Total must be greater than zero")
                .Must((model, total) => total == model.SubTotal + model.VATAmount + model.ShippingFee)
                .WithMessage("Total must equal subtotal + VAT + shipping fee");
        }
    }
}