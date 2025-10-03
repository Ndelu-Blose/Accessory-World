using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Services.AI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccessoryWorld.Services
{
    /// <summary>
    /// Service for calculating trade-in pricing with transparent formula
    /// </summary>
    public sealed class PricingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PricingService> _logger;

        public PricingService(ApplicationDbContext context, ILogger<PricingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Calculates a trade-in quote based on device assessment
        /// </summary>
        /// <param name="tradeInId">Trade-in ID</param>
        /// <param name="assessment">AI assessment result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Pricing quote with breakdown</returns>
        public async Task<TradeInQuote> QuoteAsync(
            int tradeInId, 
            DeviceAssessmentResult assessment, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Calculating quote for TradeIn {TradeInId}", tradeInId);

                // Get trade-in details
                var tradeIn = await _context.TradeIns
                    .FirstOrDefaultAsync(t => t.Id == tradeInId, cancellationToken);

                if (tradeIn == null)
                {
                    throw new ArgumentException($"TradeIn {tradeInId} not found");
                }

                // Find device in catalog
                var deviceCatalog = await FindDeviceInCatalogAsync(
                    assessment.DetectedBrand ?? tradeIn.DeviceBrand,
                    assessment.DetectedModel ?? tradeIn.DeviceModel,
                    cancellationToken);

                if (deviceCatalog == null)
                {
                    _logger.LogWarning("Device not found in catalog: {Brand} {Model}", 
                        tradeIn.DeviceBrand, tradeIn.DeviceModel);
                    return CreateFallbackQuote(tradeIn, assessment);
                }

                // Get base price
                var basePrice = await GetBasePriceAsync(deviceCatalog.Id, cancellationToken);
                if (basePrice == null)
                {
                    _logger.LogWarning("No base price found for device catalog {DeviceId}", deviceCatalog.Id);
                    return CreateFallbackQuote(tradeIn, assessment);
                }

                // Calculate pricing components
                var grade = GradeRules.ToGrade(assessment);
                var gradeMultiplier = (decimal)GradeRules.GetGradeMultiplier(grade);
                var depreciationMultiplier = (decimal)CalculateDepreciationMultiplier(deviceCatalog);
                var adjustmentRules = await ApplyAdjustmentRulesAsync(deviceCatalog, assessment, cancellationToken);

                // Apply pricing formula: BasePrice × Depreciation × Grade × Rules - Deductions
                var baseAmount = basePrice.BasePrice;
                var afterDepreciation = baseAmount * depreciationMultiplier;
                var afterGrade = afterDepreciation * gradeMultiplier;
                var afterRules = ApplyAdjustmentRules(afterGrade, adjustmentRules);
                var finalAmount = Math.Max(0, afterRules); // Ensure non-negative

                // Create detailed breakdown
                var breakdown = new PricingBreakdown
                {
                    BasePrice = baseAmount,
                    DepreciationMultiplier = depreciationMultiplier,
                    GradeMultiplier = gradeMultiplier,
                    Grade = grade,
                    GradeExplanation = GradeRules.GetGradeExplanation(assessment),
                    AdjustmentRules = adjustmentRules,
                    FinalAmount = finalAmount
                };

                var quote = new TradeInQuote
                {
                    TradeInId = tradeInId,
                    OfferAmount = finalAmount,
                    Grade = grade,
                    Breakdown = breakdown,
                    IsAcceptable = GradeRules.IsAcceptableGrade(grade) && finalAmount > 0,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Quote calculated for TradeIn {TradeInId}: R{Amount} ({Grade})", 
                    tradeInId, finalAmount, grade);

                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate quote for TradeIn {TradeInId}", tradeInId);
                throw;
            }
        }

        /// <summary>
        /// Seeds test data for iPhone 13 device model and pricing rules
        /// </summary>
        public async Task SeedTestDataAsync()
        {
            _logger.LogInformation("Seeding test data for device pricing");
        
            // Check if iPhone 13 already exists
            var existingDevice = await _context.DeviceModelCatalogs
                .FirstOrDefaultAsync(d => d.Brand == "Apple" && d.Model == "iPhone 13" && d.DeviceType == "Smartphone");
        
            if (existingDevice == null)
            {
                // Create iPhone 13 device model
                var iPhone13 = new DeviceModelCatalog
                {
                    Brand = "Apple",
                    Model = "iPhone 13",
                    DeviceType = "Smartphone",
                    ReleaseYear = 2021,
                    StorageGb = 128
                };
        
                _context.DeviceModelCatalogs.Add(iPhone13);
                await _context.SaveChangesAsync();
        
                // Add base price
                var basePrice = new DeviceBasePrice
                {
                    DeviceModelCatalogId = iPhone13.Id,
                    BasePrice = 12000.00m,
                    AsOf = DateTime.UtcNow
                };
        
                _context.DeviceBasePrices.Add(basePrice);
                _logger.LogInformation("Created iPhone 13 device model with base price R12,000");
            }
        
            // Seed price adjustment rules if they don't exist
            var existingRules = await _context.PriceAdjustmentRules.AnyAsync();
            if (!existingRules)
            {
                var rules = new List<PriceAdjustmentRule>
                {
                    new() { Code = "SCREEN_CRACK", Multiplier = 0.7m, AppliesTo = "ANY", Description = "Screen crack damage", IsActive = true },
                    new() { Code = "BODY_DAMAGE", Multiplier = 0.8m, AppliesTo = "ANY", Description = "Body damage", IsActive = true },
                    new() { Code = "WATER_DAMAGE", Multiplier = 0.5m, AppliesTo = "ANY", Description = "Water damage", IsActive = true },
                    new() { Code = "EXCELLENT_CONDITION", Multiplier = 1.0m, AppliesTo = "ANY", Description = "Excellent condition", IsActive = true },
                    new() { Code = "GOOD_CONDITION", Multiplier = 0.85m, AppliesTo = "ANY", Description = "Good condition", IsActive = true },
                    new() { Code = "FAIR_CONDITION", Multiplier = 0.65m, AppliesTo = "ANY", Description = "Fair condition", IsActive = true },
                    new() { Code = "POOR_CONDITION", Multiplier = 0.4m, AppliesTo = "ANY", Description = "Poor condition", IsActive = true }
                };
        
                _context.PriceAdjustmentRules.AddRange(rules);
                _logger.LogInformation("Created {Count} price adjustment rules", rules.Count);
            }
        
            await _context.SaveChangesAsync();
            _logger.LogInformation("Test data seeding completed");
        }

        private async Task<DeviceModelCatalog?> FindDeviceInCatalogAsync(
            string brand, 
            string model, 
            CancellationToken cancellationToken)
        {
            return await _context.DeviceModelCatalogs
                .FirstOrDefaultAsync(d => 
                    d.Brand.ToLower() == brand.ToLower() && 
                    d.Model.ToLower() == model.ToLower(), 
                    cancellationToken);
        }

        private async Task<DeviceBasePrice?> GetBasePriceAsync(int deviceCatalogId, CancellationToken cancellationToken)
        {
            return await _context.DeviceBasePrices
                .Where(p => p.DeviceModelCatalogId == deviceCatalogId)
                .OrderByDescending(p => p.AsOf)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private double CalculateDepreciationMultiplier(DeviceModelCatalog device)
        {
            var currentYear = DateTime.UtcNow.Year;
            var deviceAge = currentYear - device.ReleaseYear;
            
            // Depreciation formula: 15% per year, minimum 30%
            var depreciationRate = Math.Min(0.15 * deviceAge, 0.70);
            return Math.Max(0.30, 1.0 - depreciationRate);
        }

        private async Task<List<AppliedRule>> ApplyAdjustmentRulesAsync(
            DeviceModelCatalog device, 
            DeviceAssessmentResult assessment, 
            CancellationToken cancellationToken)
        {
            var appliedRules = new List<AppliedRule>();
            
            var rules = await _context.PriceAdjustmentRules
                .Where(r => r.IsActive && (r.AppliesTo == "ANY" || r.AppliesTo == device.DeviceType))
                .ToListAsync(cancellationToken);

            foreach (var rule in rules)
            {
                if (ShouldApplyRule(rule, assessment))
                {
                    appliedRules.Add(new AppliedRule
                    {
                        Code = rule.Code,
                        Description = rule.Description,
                        Multiplier = rule.Multiplier,
                        FlatDeduction = rule.FlatDeduction,
                        Reason = GetRuleReason(rule, assessment)
                    });
                }
            }

            return appliedRules;
        }

        private bool ShouldApplyRule(PriceAdjustmentRule rule, DeviceAssessmentResult assessment)
        {
            return rule.Code switch
            {
                "SCREEN_CRACK" => assessment.ScreenCrackSeverity > 0.1,
                "BODY_DAMAGE" => assessment.BodyDentSeverity > 0.1,
                "WATER_DAMAGE" => assessment.WaterDamageLikelihood > 0.3,
                "EXCELLENT_CONDITION" => GetOverallCondition(assessment) == "Excellent",
                "GOOD_CONDITION" => GetOverallCondition(assessment) == "Good",
                "FAIR_CONDITION" => GetOverallCondition(assessment) == "Fair",
                "POOR_CONDITION" => GetOverallCondition(assessment) == "Poor",
                _ => false
            };
        }

        private string GetRuleReason(PriceAdjustmentRule rule, DeviceAssessmentResult assessment)
        {
            return rule.Code switch
            {
                "SCREEN_CRACK" => $"Screen damage detected (severity: {assessment.ScreenCrackSeverity:P0})",
                "BODY_DAMAGE" => $"Body damage detected (severity: {assessment.BodyDentSeverity:P0})",
                "WATER_DAMAGE" => $"Water damage detected (likelihood: {assessment.WaterDamageLikelihood:P0})",
                _ => $"Overall condition: {GetOverallCondition(assessment) ?? "Unknown"}"
            };
        }

        private string GetOverallCondition(DeviceAssessmentResult assessment)
        {
            // Convert overall condition score (0.0-1.0) to grade
            return assessment.OverallConditionScore switch
            {
                >= 0.9 => "Excellent",
                >= 0.7 => "Good", 
                >= 0.5 => "Fair",
                _ => "Poor"
            };
        }

        private decimal ApplyAdjustmentRules(decimal amount, List<AppliedRule> rules)
        {
            var result = amount;
            
            foreach (var rule in rules)
            {
                result *= rule.Multiplier;
                if (rule.FlatDeduction.HasValue)
                {
                    result -= rule.FlatDeduction.Value;
                }
            }

            return result;
        }

        private TradeInQuote CreateFallbackQuote(TradeIn tradeIn, DeviceAssessmentResult assessment)
        {
            var grade = GradeRules.ToGrade(assessment);
            var fallbackAmount = 500m; // Minimum fallback amount
            
            var breakdown = new PricingBreakdown
            {
                BasePrice = fallbackAmount,
                DepreciationMultiplier = 1.0m,
                GradeMultiplier = 1.0m,
                Grade = grade,
                GradeExplanation = "Fallback pricing - device not in catalog",
                AdjustmentRules = new List<AppliedRule>(),
                FinalAmount = fallbackAmount
            };

            return new TradeInQuote
            {
                TradeInId = tradeIn.Id,
                OfferAmount = fallbackAmount,
                Grade = grade,
                Breakdown = breakdown,
                IsAcceptable = GradeRules.IsAcceptableGrade(grade),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Trade-in pricing quote result
    /// </summary>
    public sealed class TradeInQuote
    {
        public int TradeInId { get; set; }
        public decimal OfferAmount { get; set; }
        public string Grade { get; set; } = string.Empty;
        public PricingBreakdown Breakdown { get; set; } = new();
        public bool IsAcceptable { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Detailed breakdown of pricing calculation
    /// </summary>
    public sealed class PricingBreakdown
    {
        public decimal BasePrice { get; set; }
        public decimal DepreciationMultiplier { get; set; }
        public decimal GradeMultiplier { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string GradeExplanation { get; set; } = string.Empty;
        public List<AppliedRule> AdjustmentRules { get; set; } = new();
        public decimal FinalAmount { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
        }
    }

    /// <summary>
    /// Applied pricing adjustment rule
    /// </summary>
    public sealed class AppliedRule
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Multiplier { get; set; } = 1.0m;
        public decimal? FlatDeduction { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}