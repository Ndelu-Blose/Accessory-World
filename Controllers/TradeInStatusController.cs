using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AccessoryWorld.Controllers
{
    /// <summary>
    /// Provides real-time status tracking for trade-in submissions
    /// </summary>
    [Route("TradeIn/Status")]
    public class TradeInStatusController : Controller
    {
        private readonly Data.ApplicationDbContext _db;

        public TradeInStatusController(Data.ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Gets the current status of a trade-in by its public ID
        /// </summary>
        /// <param name="publicId">The public ID of the trade-in</param>
        /// <returns>JSON object containing current status and relevant details</returns>
        [HttpGet("{publicId}")]
        public async Task<IActionResult> GetStatus(System.Guid publicId)
        {
            var tradeIn = await _db.TradeIns
                .AsNoTracking()
                .Include(x => x.CreditNote)
                .SingleOrDefaultAsync(x => x.PublicId == publicId);

            if (tradeIn == null)
            {
                return NotFound(new { error = "Trade-in not found" });
            }

            var hasOffer = tradeIn.AutoOfferAmount.HasValue || tradeIn.ApprovedValue.HasValue;
            var offerAmount = tradeIn.ApprovedValue ?? tradeIn.AutoOfferAmount;

            return Json(new
            {
                status = tradeIn.Status,
                hasOffer,
                offer = hasOffer ? offerAmount : null,
                grade = tradeIn.AutoGrade ?? tradeIn.ConditionGrade,
                aiVendor = tradeIn.AiVendor,
                aiConfidence = tradeIn.AiConfidence,
                aiRetryCount = tradeIn.AiRetryCount,
                createdAt = tradeIn.CreatedAt,
                reviewedAt = tradeIn.ReviewedAt,
                approvedBy = tradeIn.ApprovedBy,
                notes = tradeIn.Notes,
                deviceInfo = new
                {
                    brand = tradeIn.DeviceBrand,
                    model = tradeIn.DeviceModel,
                    type = tradeIn.DeviceType,
                    imei = tradeIn.IMEI
                },
                creditNote = tradeIn.CreditNote != null ? new
                {
                    code = tradeIn.CreditNote.CreditNoteCode,
                    amount = tradeIn.CreditNote.Amount,
                    remainingAmount = tradeIn.CreditNote.AmountRemaining,
                    status = tradeIn.CreditNote.Status,
                    expiresAt = tradeIn.CreditNote.ExpiresAt
                } : null
            });
        }

        /// <summary>
        /// Gets a simplified status for polling (lighter payload)
        /// </summary>
        /// <param name="publicId">The public ID of the trade-in</param>
        /// <returns>JSON object with minimal status information for polling</returns>
        [HttpGet("{publicId}/simple")]
        public async Task<IActionResult> GetSimpleStatus(System.Guid publicId)
        {
            var tradeIn = await _db.TradeIns
                .AsNoTracking()
                .Where(x => x.PublicId == publicId)
                .Select(x => new
                {
                    x.Status,
                    x.AutoOfferAmount,
                    x.ApprovedValue,
                    x.AiRetryCount,
                    x.ReviewedAt
                })
                .SingleOrDefaultAsync();

            if (tradeIn == null)
            {
                return NotFound(new { error = "Trade-in not found" });
            }

            var hasOffer = tradeIn.AutoOfferAmount.HasValue || tradeIn.ApprovedValue.HasValue;
            var offerAmount = tradeIn.ApprovedValue ?? tradeIn.AutoOfferAmount;

            return Json(new
            {
                status = tradeIn.Status,
                hasOffer,
                offer = hasOffer ? offerAmount : null,
                aiRetryCount = tradeIn.AiRetryCount,
                lastUpdated = tradeIn.ReviewedAt
            });
        }
    }
}