using AccessoryWorld.ViewModels;
using FluentValidation;

namespace AccessoryWorld.Validators
{
    public class PaymentViewModelValidator : AbstractValidator<PaymentViewModel>
    {
        public PaymentViewModelValidator()
        {
            RuleFor(x => x.Order)
                .NotNull().WithMessage("Order information is required");

            RuleFor(x => x.OrderItems)
                .NotEmpty().WithMessage("Order must contain at least one item")
                .Must(items => items.All(item => item.Quantity > 0 && item.UnitPrice > 0))
                .WithMessage("All order items must have positive quantity and unit price");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty().WithMessage("Payment method is required")
                .Must(method => method == "card" || method == "eft")
                .WithMessage("Payment method must be either 'card' or 'eft'");

            // Card payment validation
            When(x => x.PaymentMethod == "card", () => {
                RuleFor(x => x.CardNumber)
                    .NotEmpty().WithMessage("Card number is required for card payments")
                    .CreditCard().WithMessage("Invalid credit card number")
                    .Length(13, 19).WithMessage("Card number must be between 13 and 19 digits");

                RuleFor(x => x.CardHolderName)
                    .NotEmpty().WithMessage("Card holder name is required for card payments")
                    .MaximumLength(100).WithMessage("Card holder name cannot exceed 100 characters")
                    .Matches(@"^[a-zA-Z\s]+$").WithMessage("Card holder name can only contain letters and spaces");

                RuleFor(x => x.ExpiryMonth)
                    .NotEmpty().WithMessage("Expiry month is required for card payments")
                    .Must(month => int.TryParse(month, out int m) && m >= 1 && m <= 12)
                    .WithMessage("Expiry month must be between 1 and 12");

                RuleFor(x => x.ExpiryYear)
                    .NotEmpty().WithMessage("Expiry year is required for card payments")
                    .Must(year => int.TryParse(year, out int y) && y >= DateTime.Now.Year && y <= DateTime.Now.Year + 10)
                    .WithMessage($"Expiry year must be between {DateTime.Now.Year} and {DateTime.Now.Year + 10}");

                RuleFor(x => x.CVV)
                    .NotEmpty().WithMessage("CVV is required for card payments")
                    .Length(3, 4).WithMessage("CVV must be 3 or 4 digits")
                    .Matches(@"^\d{3,4}$").WithMessage("CVV must contain only digits");
            });

            // EFT payment validation
            When(x => x.PaymentMethod == "eft", () => {
                RuleFor(x => x.BankName)
                    .NotEmpty().WithMessage("Bank name is required for EFT payments")
                    .MaximumLength(100).WithMessage("Bank name cannot exceed 100 characters");

                RuleFor(x => x.AccountHolder)
                    .NotEmpty().WithMessage("Account holder name is required for EFT payments")
                    .MaximumLength(100).WithMessage("Account holder name cannot exceed 100 characters")
                    .Matches(@"^[a-zA-Z\s]+$").WithMessage("Account holder name can only contain letters and spaces");

                RuleFor(x => x.AccountNumber)
                    .NotEmpty().WithMessage("Account number is required for EFT payments")
                    .Length(8, 20).WithMessage("Account number must be between 8 and 20 digits")
                    .Matches(@"^\d+$").WithMessage("Account number must contain only digits");

                RuleFor(x => x.BranchCode)
                    .NotEmpty().WithMessage("Branch code is required for EFT payments")
                    .Length(6).WithMessage("Branch code must be exactly 6 digits")
                    .Matches(@"^\d{6}$").WithMessage("Branch code must be exactly 6 digits");
            });

            // Pickup OTP validation
            RuleFor(x => x.PickupOTP)
                .Length(6).WithMessage("Pickup OTP must be exactly 6 digits")
                .Matches(@"^\d{6}$").WithMessage("Pickup OTP must be exactly 6 digits")
                .When(x => !string.IsNullOrEmpty(x.PickupOTP));
        }
    }
}