using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AccessoryWorld.Controllers
{
    // Minimal JSON endpoint the Details view can poll
    [Route("TradeIn/Status")]
    public class TradeInStatusController : Controller
    {
        private readonly Data.ApplicationDbContext _db;
        public TradeInStatusController(Data.ApplicationDbContext db) => _db = db;

        [HttpGet("{publicId}")]
        public async Task<IActionResult> GetStatus(System.Guid publicId)
        {
            var t = await _db.TradeIns
                .AsNoTracking()
                .Include(x => x.CreditNote) // if you have it
                .SingleOrDefaultAsync(x => x.PublicId == publicId);

            if (t == null) return NotFound();

            var hasOffer = t.AutoOfferAmount.HasValue;
            return Json(new
            {
                status = t.Status.ToString(),
                hasOffer,
                offer = hasOffer ? t.AutoOfferAmount : null,
                grade = t.AutoGrade,
                aiVendor = t.AiVendor,
                aiConfidence = t.AiConfidence
            });
        }
    }
}
