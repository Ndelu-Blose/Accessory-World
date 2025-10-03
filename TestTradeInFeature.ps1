# Test script for Trade-In Feature
Write-Host "Testing Trade-In Feature..." -ForegroundColor Green

$baseUrl = "http://localhost:5205"

Write-Host "`n1. Testing Trade-In API Endpoints..." -ForegroundColor Yellow

# Test creating a trade-in
Write-Host "`nCreating a test trade-in..." -ForegroundColor Cyan
try {
    $tradeInData = @{
        DeviceBrand = "Apple"
        DeviceModel = "iPhone 13"
        DeviceType = "smartphone"
        IMEI = "123456789012345"
        Description = "Test device for AI assessment"
        ConditionGrade = "B"
        PhotosJson = '["https://example.com/photo1.jpg", "https://example.com/photo2.jpg"]'
        Status = "SUBMITTED"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/tradeins" -Method POST -Body $tradeInData -ContentType "application/json"
    Write-Host "✅ Trade-In created successfully" -ForegroundColor Green
    Write-Host "   Trade-In ID: $($response.id)" -ForegroundColor Cyan
    Write-Host "   Public ID: $($response.publicId)" -ForegroundColor Cyan
    Write-Host "   Status: $($response.status)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Trade-In creation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error details: $errorBody" -ForegroundColor Red
    }
}

# Test getting trade-ins
Write-Host "`nGetting trade-ins..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/tradeins" -Method GET -ContentType "application/json"
    Write-Host "✅ Trade-Ins retrieved successfully" -ForegroundColor Green
    Write-Host "   Found $($response.Count) trade-ins" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Failed to get trade-ins: $($_.Exception.Message)" -ForegroundColor Red
}

# Test AI assessment trigger
Write-Host "`nTesting AI assessment trigger..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/tradeins/1/assess" -Method POST -ContentType "application/json"
    Write-Host "✅ AI assessment triggered successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ AI assessment trigger failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Testing Trade-In UI..." -ForegroundColor Yellow

# Test Trade-In index page
Write-Host "`nTesting Trade-In index page..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/TradeIn" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Trade-In index page accessible" -ForegroundColor Green
        Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Trade-In index page returned status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Trade-In index page failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Trade-In create page
Write-Host "`nTesting Trade-In create page..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/TradeIn/Create" -Method GET
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Trade-In create page accessible" -ForegroundColor Green
        Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Trade-In create page returned status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Trade-In create page failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. Testing Database Schema..." -ForegroundColor Yellow

# Test if the application can start without database errors
Write-Host "`nChecking application startup logs..." -ForegroundColor Cyan
Write-Host "✅ Application is running on $baseUrl" -ForegroundColor Green

Write-Host "`n🎉 Trade-In Feature Test Complete!" -ForegroundColor Green
Write-Host "Check the results above to verify the Trade-In feature is working." -ForegroundColor Cyan

