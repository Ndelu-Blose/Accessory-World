using AccessoryWorld.Services.AI;

namespace AccessoryWorld.Services
{
    /// <summary>
    /// Deterministic grade mapping rules based on AI assessment results
    /// </summary>
    public static class GradeRules
    {
        /// <summary>
        /// Maps AI assessment result to a letter grade (A-D)
        /// </summary>
        /// <param name="assessment">AI assessment result</param>
        /// <returns>Letter grade from A (excellent) to D (poor)</returns>
        public static string ToGrade(DeviceAssessmentResult assessment)
        {
            // Calculate weighted damage score (0.0 = perfect, 1.0 = severely damaged)
            var damageScore = CalculateWeightedDamageScore(assessment);
            
            // Map damage score to letter grade
            return damageScore switch
            {
                <= 0.15 => "A", // Excellent condition (0-15% damage)
                <= 0.35 => "B", // Good condition (16-35% damage)
                <= 0.60 => "C", // Fair condition (36-60% damage)
                _ => "D"        // Poor condition (61%+ damage)
            };
        }

        /// <summary>
        /// Gets a detailed explanation of the grade assignment
        /// </summary>
        /// <param name="assessment">AI assessment result</param>
        /// <returns>Human-readable explanation of the grade</returns>
        public static string GetGradeExplanation(DeviceAssessmentResult assessment)
        {
            var grade = ToGrade(assessment);
            var damageScore = CalculateWeightedDamageScore(assessment);
            
            var explanation = grade switch
            {
                "A" => "Excellent condition - minimal wear and tear",
                "B" => "Good condition - minor cosmetic issues",
                "C" => "Fair condition - noticeable damage but functional",
                "D" => "Poor condition - significant damage affecting value",
                _ => "Unable to determine grade"
            };

            var issues = new List<string>();
            
            // Add specific damage details
            if (assessment.ScreenCrackSeverity > 0.3)
                issues.Add($"Screen damage detected ({assessment.ScreenCrackSeverity:P0})");
            
            if (assessment.BodyDentSeverity > 0.3)
                issues.Add($"Body damage detected ({assessment.BodyDentSeverity:P0})");
            
            if (assessment.BackGlassSeverity > 0.3)
                issues.Add($"Back glass damage detected ({assessment.BackGlassSeverity:P0})");
            
            if (assessment.CameraDamageSeverity > 0.3)
                issues.Add($"Camera damage detected ({assessment.CameraDamageSeverity:P0})");
            
            if (assessment.WaterDamageLikelihood > 0.5)
                issues.Add($"Possible water damage ({assessment.WaterDamageLikelihood:P0} likelihood)");

            if (issues.Any())
            {
                explanation += $". Issues found: {string.Join(", ", issues)}";
            }

            return explanation;
        }

        /// <summary>
        /// Calculates a weighted damage score from AI assessment
        /// </summary>
        /// <param name="assessment">AI assessment result</param>
        /// <returns>Weighted damage score (0.0 = perfect, 1.0 = severely damaged)</returns>
        private static double CalculateWeightedDamageScore(DeviceAssessmentResult assessment)
        {
            // Weight factors for different types of damage
            const double screenWeight = 0.35;      // Screen damage is most impactful
            const double bodyWeight = 0.20;        // Body damage affects aesthetics
            const double backGlassWeight = 0.15;   // Back glass damage is noticeable
            const double cameraWeight = 0.15;      // Camera damage affects functionality
            const double waterWeight = 0.15;       // Water damage is serious but uncertain

            var weightedScore = 
                (assessment.ScreenCrackSeverity * screenWeight) +
                (assessment.BodyDentSeverity * bodyWeight) +
                (assessment.BackGlassSeverity * backGlassWeight) +
                (assessment.CameraDamageSeverity * cameraWeight) +
                (assessment.WaterDamageLikelihood * waterWeight);

            // Also consider overall condition score if it's worse than calculated damage
            var overallDamage = 1.0 - assessment.OverallConditionScore;
            
            // Take the worse of the two scores
            return Math.Max(weightedScore, overallDamage);
        }

        /// <summary>
        /// Gets the grade multiplier for pricing calculations
        /// </summary>
        /// <param name="grade">Letter grade (A-D)</param>
        /// <returns>Multiplier for base price (1.0 = full price, 0.0 = no value)</returns>
        public static double GetGradeMultiplier(string grade)
        {
            return grade switch
            {
                "A" => 0.90, // 90% of base price for excellent condition
                "B" => 0.75, // 75% of base price for good condition
                "C" => 0.55, // 55% of base price for fair condition
                "D" => 0.30, // 30% of base price for poor condition
                _ => 0.20    // 20% of base price for unknown/ungraded
            };
        }

        /// <summary>
        /// Determines if a grade is acceptable for trade-in
        /// </summary>
        /// <param name="grade">Letter grade (A-D)</param>
        /// <returns>True if the device is acceptable for trade-in</returns>
        public static bool IsAcceptableGrade(string grade)
        {
            // All grades A-D are acceptable, but D grade gets very low value
            return grade is "A" or "B" or "C" or "D";
        }
    }
}