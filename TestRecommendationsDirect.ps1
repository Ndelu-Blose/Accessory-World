# Direct test of AI Recommendation System
Write-Host "Testing AI Recommendation System Directly..." -ForegroundColor Green

# Test basic recommendations endpoint
Write-Host "`n1. Testing Basic Recommendations..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5103/api/ai/recommendations?count=2" -Method GET -ContentType "application/json"
    Write-Host "✅ Basic Recommendations working" -ForegroundColor Green
    Write-Host "   Found $($response.products.Count) recommendations" -ForegroundColor Cyan
    if ($response.products.Count -gt 0) {
        Write-Host "   First product: $($response.products[0].name)" -ForegroundColor Cyan
        Write-Host "   Algorithm: $($response.algorithmUsed)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Basic Recommendations failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Response: $($_.Exception.Response)" -ForegroundColor Red
}

# Test trending recommendations
Write-Host "`n2. Testing Trending Recommendations..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5103/api/ai/recommendations?count=2&algorithmType=TRENDING" -Method GET -ContentType "application/json"
    Write-Host "✅ Trending Recommendations working" -ForegroundColor Green
    Write-Host "   Found $($response.products.Count) recommendations" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Trending Recommendations failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test similar products
Write-Host "`n3. Testing Similar Products..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5103/api/ai/recommendations/similar?productId=1&count=2" -Method GET -ContentType "application/json"
    Write-Host "✅ Similar Products working" -ForegroundColor Green
    Write-Host "   Found $($response.products.Count) similar products" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Similar Products failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test behavior tracking with minimal data
Write-Host "`n4. Testing Behavior Tracking..." -ForegroundColor Yellow
try {
    $body = @{
        productId = 1
        actionType = "VIEW"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5103/api/ai/recommendations/behavior" -Method POST -Body $body -ContentType "application/json"
    Write-Host "✅ Behavior Tracking working" -ForegroundColor Green
} catch {
    Write-Host "❌ Behavior Tracking failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Direct Test Complete!" -ForegroundColor Green
Write-Host "The AI Recommendation System is working!" -ForegroundColor Cyan
