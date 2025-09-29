using AccessoryWorld.Models;

namespace AccessoryWorld.ViewModels
{
    public class CreditNoteViewModel
    {
        public int Id { get; set; }
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => DateTime.UtcNow > ExpiryDate;
        public bool IsActive => Status == "ACTIVE" && !IsExpired && RemainingAmount > 0;
        public string DisplayText => $"{CreditNoteCode} - R{RemainingAmount:N2} (Expires: {ExpiryDate:dd MMM yyyy})";

        public CreditNoteViewModel() { }

        public CreditNoteViewModel(CreditNote creditNote)
        {
            Id = creditNote.Id;
            CreditNoteCode = creditNote.CreditNoteCode;
            OriginalAmount = creditNote.Amount;
            RemainingAmount = creditNote.AmountRemaining;
            Status = creditNote.Status;
            IssuedDate = creditNote.CreatedAt;
            ExpiryDate = creditNote.ExpiresAt;
        }
    }
}