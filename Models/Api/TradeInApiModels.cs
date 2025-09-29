using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models.Api
{
    // Trade-In Request/Response Models
    public class CreateTradeInRequest
    {
        [Required]
        public string ProductName { get; set; } = string.Empty;
        
        [Required]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        public string Model { get; set; } = string.Empty;
        
        public string? SerialNumber { get; set; }
        
        [Required]
        public string ConditionDescription { get; set; } = string.Empty;
        
        public List<string> ImageUrls { get; set; } = new();
        
        public string? AdditionalNotes { get; set; }
        
        [Required]
        public string ContactEmail { get; set; } = string.Empty;
        
        public string? ContactPhone { get; set; }
    }

    public class UpdateTradeInStatusRequest
    {
        [Required]
        public string NewStatus { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
    }

    public class EvaluateTradeInRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Estimated value must be greater than 0")]
        public decimal EstimatedValue { get; set; }
        
        [Required]
        public string ConditionGrade { get; set; } = string.Empty;
        
        public string? EvaluationNotes { get; set; }
        
        public DateTime? OfferExpiryDate { get; set; }
    }

    public class TradeInResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string ConditionDescription { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? EstimatedValue { get; set; }
        public string? ConditionGrade { get; set; }
        public string? EvaluationNotes { get; set; }
        public DateTime? OfferExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? AdditionalNotes { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public List<TradeInImageResponse> Images { get; set; } = new();
        public TradeInEvaluationResponse? Evaluation { get; set; }
        public CreditNoteResponse? CreditNote { get; set; }

        public TradeInResponse() { }

        public TradeInResponse(TradeIn tradeIn)
        {
            Id = tradeIn.Id;
            UserId = tradeIn.CustomerId;
            ProductName = tradeIn.DeviceModel;
            Brand = tradeIn.DeviceBrand;
            Model = tradeIn.DeviceModel;
            SerialNumber = tradeIn.IMEI;
            ConditionDescription = tradeIn.ConditionGrade;
            Status = tradeIn.Status;
            EstimatedValue = tradeIn.ProposedValue;
            ConditionGrade = tradeIn.ConditionGrade;
            EvaluationNotes = tradeIn.Notes;
            OfferExpiryDate = null; // TradeIn doesn't have ExpiresAt, only TradeInCase does
            CreatedAt = tradeIn.CreatedAt;
            UpdatedAt = tradeIn.CreatedAt; // TradeIn doesn't have UpdatedAt, using CreatedAt
            AdditionalNotes = tradeIn.Notes;
            ContactEmail = string.Empty; // Not available in TradeIn model
            ContactPhone = string.Empty; // Not available in TradeIn model
            Images = new List<TradeInImageResponse>(); // Images stored as JSON, would need parsing
            Evaluation = null; // Evaluation is not a separate entity
            CreditNote = tradeIn.CreditNote != null ? new CreditNoteResponse(tradeIn.CreditNote) : null;
        }
    }

    public class TradeInImageResponse
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime UploadedAt { get; set; }

        public TradeInImageResponse() { }

        public TradeInImageResponse(TradeInImage image)
        {
            Id = Guid.NewGuid(); // Generate new Guid since TradeInImage.Id is int
            ImageUrl = image.ImageUrl;
            Description = image.Description;
            DisplayOrder = 0; // TradeInImage doesn't have DisplayOrder
            UploadedAt = image.CreatedAt;
        }
    }

    public class TradeInEvaluationResponse
    {
        public Guid Id { get; set; }
        public decimal EstimatedValue { get; set; }
        public string ConditionGrade { get; set; } = string.Empty;
        public string? EvaluationNotes { get; set; }
        public string EvaluatedBy { get; set; } = string.Empty;
        public DateTime EvaluatedAt { get; set; }

        public TradeInEvaluationResponse() { }

        public TradeInEvaluationResponse(TradeInEvaluation evaluation)
        {
            Id = Guid.NewGuid(); // Generate new Guid since TradeInEvaluation.Id is int
            EstimatedValue = evaluation.FinalOfferAmount;
            ConditionGrade = "N/A"; // TradeInEvaluation doesn't have ConditionGrade
            EvaluationNotes = evaluation.EvaluatorNotes;
            EvaluatedBy = evaluation.EvaluatedByUserId;
            EvaluatedAt = evaluation.EvaluatedAt;
        }
    }

    public class CreditNoteResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int? ConsumedInOrderId { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public int TradeInId { get; set; }

        public CreditNoteResponse() { }

        public CreditNoteResponse(CreditNote creditNote)
        {
            Id = creditNote.Id;
            UserId = creditNote.UserId;
            CreditNoteCode = creditNote.CreditNoteCode;
            OriginalAmount = creditNote.Amount;
            RemainingAmount = creditNote.AmountRemaining;
            Status = creditNote.Status;
            IssuedDate = creditNote.CreatedAt;
            ExpiryDate = creditNote.ExpiresAt;
            ConsumedInOrderId = creditNote.ConsumedInOrderId;
            ConsumedAt = creditNote.RedeemedAt;
            TradeInId = creditNote.TradeInId;
        }
    }

    public class TradeInStatsResponse
    {
        public int TotalTradeIns { get; set; }
        public int PendingTradeIns { get; set; }
        public int EvaluatedTradeIns { get; set; }
        public int AcceptedTradeIns { get; set; }
        public int RejectedTradeIns { get; set; }
        public decimal TotalValueOffered { get; set; }
        public decimal TotalValueAccepted { get; set; }
        public int ActiveCreditNotes { get; set; }
        public decimal TotalCreditIssued { get; set; }
        public decimal TotalCreditUsed { get; set; }
        public decimal TotalCreditRemaining { get; set; }
    }

    // Pagination and filtering
    public class TradeInQueryRequest
    {
        public string? Status { get; set; }
        public string? UserId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    // Upload models
    public class UploadImageRequest
    {
        [Required]
        public IFormFile Image { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
    }

    public class UploadImageResponse
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Bulk operations
    public class BulkUpdateTradeInStatusRequest
    {
        [Required]
        public List<Guid> TradeInIds { get; set; } = new();
        
        [Required]
        public string NewStatus { get; set; } = string.Empty;
        
        public string? Notes { get; set; }
    }

    public class BulkUpdateResponse
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class ValidateCreditNoteRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        public decimal? RequestedAmount { get; set; }
    }

    public class ValidateCreditNoteResponse
    {
        public bool IsValid { get; set; }
        public int? CreditNoteId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
        public decimal MaxApplicableAmount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}