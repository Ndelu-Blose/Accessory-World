# Test script for AI Recommendation System
Write-Host "Testing AI Recommendation System..." -ForegroundColor Green

# Test API endpoints
$baseUrl = "http://localhost:5103"

Write-Host "`n1. Testing Recommendations API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/ai/recommendations?count=3&algorithmType=TRENDING" -Method GET -ContentType "application/json"
    Write-Host "✅ Recommendations API working" -ForegroundColor Green
    Write-Host "   Found $($response.products.Count) recommendations" -ForegroundColor Cyan
    Write-Host "   Algorithm used: $($response.algorithmUsed)" -ForegroundColor Cyan
    Write-Host "   Processing time: $($response.processingTimeMs)ms" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Recommendations API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Testing Similar Products API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/ai/recommendations/similar?productId=1&count=2" -Method GET -ContentType "application/json"
    Write-Host "✅ Similar Products API working" -ForegroundColor Green
    Write-Host "   Found $($response.products.Count) similar products" -ForegroundColor Cyan
    Write-Host "   Similarity type: $($response.similarityType)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Similar Products API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Testing Behavior Tracking API..." -ForegroundColor Yellow
try {
    $body = @{
        productId = 1
        actionType = "VIEW"
        category = "Electronics"
        brand = "Test Brand"
        price = 99.99
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/ai/recommendations/behavior" -Method POST -Body $body -ContentType "application/json"
    Write-Host "✅ Behavior Tracking API working" -ForegroundColor Green
} catch {
    Write-Host "❌ Behavior Tracking API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n4. Testing User Profile API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/ai/recommendations/profile" -Method GET -ContentType "application/json"
    Write-Host "✅ User Profile API working" -ForegroundColor Green
    Write-Host "   User ID: $($response.userId)" -ForegroundColor Cyan
    Write-Host "   Preferred categories: $($response.preferredCategories -join ', ')" -ForegroundColor Cyan
} catch {
    Write-Host "❌ User Profile API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n5. Testing A/B Test Assignment..." -ForegroundColor Yellow
try {
    $body = @{
        testName = "RecommendationAlgorithm"
        description = "Test different recommendation algorithms"
        variants = @("A", "B")
        trafficAllocation = 1.0
        startDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
        targetUserSegments = @()
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/ai/recommendations/ab-test" -Method POST -Body $body -ContentType "application/json"
    Write-Host "✅ A/B Test API working" -ForegroundColor Green
    Write-Host "   Test ID: $($response.testId)" -ForegroundColor Cyan
    Write-Host "   Assigned variant: $($response.assignedVariant)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ A/B Test API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 AI Recommendation System Test Complete!" -ForegroundColor Green
Write-Host "Check the results above to verify all endpoints are working correctly." -ForegroundColor Cyan
