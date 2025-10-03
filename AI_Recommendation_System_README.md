# AI-Powered Product Recommendation System

## Overview

The AI Recommendation System for AccessoryWorld provides intelligent, personalized product recommendations using multiple machine learning algorithms including collaborative filtering, content-based filtering, and hybrid approaches.

## Features

### 🤖 Core AI Capabilities
- **Collaborative Filtering**: Recommends products based on similar users' preferences
- **Content-Based Filtering**: Recommends products based on user's past preferences and product attributes
- **Hybrid Recommendations**: Combines multiple algorithms for optimal results
- **Trending Products**: Shows popular and trending items
- **Similar Products**: Finds products similar to a given item

### 📊 Analytics & Learning
- **User Behavior Tracking**: Tracks views, cart additions, purchases, and other interactions
- **Recommendation Feedback**: Collects user feedback on recommendations
- **Performance Metrics**: Monitors click-through rates, conversion rates, and recommendation quality
- **A/B Testing**: Supports A/B testing for different recommendation algorithms

### ⚡ Performance & Optimization
- **Intelligent Caching**: 5-minute cache for recommendations, 10-minute cache for similar products
- **Real-time Updates**: User profiles update automatically based on behavior
- **Scalable Architecture**: Designed to handle high-volume recommendation requests

## Architecture

### Models

#### UserBehavior
Tracks user interactions with products:
- `ActionType`: VIEW, ADD_TO_CART, PURCHASE, WISHLIST, SEARCH
- `ProductId`: The product being interacted with
- `SearchQuery`: Search terms used
- `Category`/`Brand`: Product categorization
- `Price`/`Quantity`: Transaction details
- `SessionId`: Session tracking
- `DeviceType`: MOBILE, DESKTOP, TABLET

#### RecommendationModel
Stores generated recommendations:
- `UserId`: Target user
- `ProductId`: Recommended product
- `AlgorithmType`: COLLABORATIVE, CONTENT_BASED, HYBRID, TRENDING
- `Score`: Recommendation confidence (0.0-1.0)
- `Rank`: Position in recommendation list
- `Reason`: Human-readable explanation
- `TestGroup`/`TestVariant`: A/B testing support

#### UserProfile
Maintains user preferences and behavior patterns:
- `PreferredCategories`: User's favorite product categories
- `PreferredBrands`: User's favorite brands
- `PriceRange`: LOW, MEDIUM, HIGH, LUXURY
- `ShoppingStyle`: BUDGET, PREMIUM, TRENDY, CLASSIC
- `AverageOrderValue`: Calculated from purchase history
- `PurchaseFrequency`: Days between purchases

#### ProductSimilarity
Pre-calculated product similarities:
- `ProductId1`/`ProductId2`: Product pair
- `SimilarityScore`: Similarity rating (0.0-1.0)
- `SimilarityType`: CONTENT, COLLABORATIVE, VISUAL

### Services

#### AIRecommendationService
Main service handling all recommendation logic:

```csharp
// Get personalized recommendations
var recommendations = await service.GetRecommendationsAsync(new RecommendationRequest
{
    UserId = "user123",
    Count = 6,
    AlgorithmType = "HYBRID",
    MaxPrice = 1000,
    PreferredCategories = new[] { "Electronics", "Accessories" }
});

// Get similar products
var similar = await service.GetSimilarProductsAsync(new SimilarProductRequest
{
    ProductId = 123,
    Count = 4,
    SimilarityType = "CONTENT"
});

// Track user behavior
await service.TrackUserBehaviorAsync(new UserBehaviorRequest
{
    UserId = "user123",
    ProductId = 456,
    ActionType = "VIEW",
    Category = "Electronics"
});
```

### API Endpoints

#### GET /api/ai/recommendations
Get personalized recommendations:
- `userId`: User ID (optional, uses authenticated user if not provided)
- `count`: Number of recommendations (default: 6)
- `algorithmType`: COLLABORATIVE, CONTENT_BASED, HYBRID, TRENDING
- `excludeProductIds`: Products to exclude
- `preferredCategories`: Preferred categories
- `preferredBrands`: Preferred brands
- `maxPrice`/`minPrice`: Price range filters

#### GET /api/ai/recommendations/similar
Get similar products:
- `productId`: Source product ID
- `count`: Number of similar products (default: 4)
- `similarityType`: CONTENT, COLLABORATIVE, VISUAL

#### POST /api/ai/recommendations/behavior
Track user behavior:
```json
{
  "productId": 123,
  "actionType": "VIEW",
  "searchQuery": "iPhone case",
  "category": "Electronics",
  "brand": "Apple",
  "price": 29.99,
  "quantity": 1
}
```

#### POST /api/ai/recommendations/feedback
Record recommendation feedback:
```json
{
  "recommendationId": 456,
  "feedbackType": "CLICKED",
  "comment": "Great recommendation!"
}
```

#### GET /api/ai/recommendations/profile
Get user profile for recommendations

#### PUT /api/ai/recommendations/profile
Update user profile:
```json
{
  "preferredCategories": ["Electronics", "Accessories"],
  "preferredBrands": ["Apple", "Samsung"],
  "priceRange": "MEDIUM",
  "shoppingStyle": "PREMIUM"
}
```

## Frontend Integration

### JavaScript Service
The `AIRecommendationsService` class provides easy integration:

```javascript
// Get recommendations
const recommendations = await AIRecommendations.getRecommendations({
    count: 6,
    algorithmType: 'HYBRID',
    maxPrice: 1000
});

// Track behavior
await AIRecommendations.trackBehavior({
    productId: 123,
    actionType: 'VIEW',
    category: 'Electronics'
});

// Get similar products
const similar = await AIRecommendations.getSimilarProducts(123, {
    count: 4,
    similarityType: 'CONTENT'
});
```

### Razor Partial View
Use `_AIRecommendations.cshtml` to display recommendations:

```html
@await Html.PartialAsync("_AIRecommendations", Model.Recommendations)
```

### Auto-tracking
The system automatically tracks:
- Page views on product detail pages
- User interactions with recommended products
- Search queries and filters
- Device type and session information

## Algorithm Details

### Collaborative Filtering
1. Find users with similar purchase patterns
2. Identify products liked by similar users
3. Exclude products already purchased by current user
4. Rank by popularity and similarity score

### Content-Based Filtering
1. Analyze user's preferred categories and brands
2. Filter products by user preferences
3. Apply price range and other filters
4. Rank by sales count and special flags

### Hybrid Approach
1. Combine collaborative and content-based results
2. Weight algorithms based on user's purchase history
3. Remove duplicates and re-rank
4. Fall back to trending products if needed

### Similarity Calculation
- **Category Match**: 40% weight
- **Brand Match**: 30% weight
- **Price Similarity**: 20% weight (within 20% range)
- **Popularity**: 10% weight

## Performance Optimization

### Caching Strategy
- **Recommendations**: 5-minute cache
- **Similar Products**: 10-minute cache
- **User Profiles**: Updated asynchronously
- **Cache Keys**: Include all relevant parameters

### Database Optimization
- Indexed columns for fast queries
- Efficient joins and filtering
- Pagination for large result sets
- Background processing for heavy calculations

### Memory Management
- Lazy loading of related data
- Efficient data structures
- Garbage collection friendly code

## Monitoring & Analytics

### Key Metrics
- **Click-Through Rate (CTR)**: Clicks / Total Recommendations
- **Conversion Rate**: Purchases / Total Recommendations
- **Average Score**: Mean recommendation confidence
- **Algorithm Performance**: Per-algorithm metrics

### A/B Testing
- Test different algorithms
- Compare recommendation strategies
- Measure user engagement
- Optimize based on results

## Configuration

### appsettings.json
```json
{
  "AIRecommendations": {
    "CacheTimeoutMinutes": 5,
    "SimilarProductsCacheTimeoutMinutes": 10,
    "MaxRecommendations": 20,
    "DefaultAlgorithm": "HYBRID",
    "EnableBehaviorTracking": true,
    "EnableA/BTesting": true
  }
}
```

## Database Migration

Run the migration to create the AI recommendation tables:

```bash
dotnet ef database update
```

This creates the following tables:
- `UserBehaviors`
- `RecommendationModels`
- `RecommendationFeedbacks`
- `ProductSimilarities`
- `UserProfiles`

## Usage Examples

### Basic Recommendations
```csharp
// In a controller
var recommendations = await _aiRecommendationService.GetRecommendationsAsync(
    new RecommendationRequest 
    { 
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        Count = 6 
    });
```

### Product Detail Page
```html
<!-- Include recommendations on product detail page -->
<div class="similar-products-section">
    <h3>Similar Products</h3>
    @await Html.PartialAsync("_AIRecommendations", Model.SimilarProducts)
</div>
```

### Homepage Recommendations
```html
<!-- Personalized recommendations on homepage -->
<div class="recommended-for-you">
    <h3>Recommended for You</h3>
    @await Html.PartialAsync("_AIRecommendations", Model.PersonalizedRecommendations)
</div>
```

## Future Enhancements

### Planned Features
- **Deep Learning Models**: Neural network-based recommendations
- **Real-time Learning**: Update models based on real-time behavior
- **Visual Similarity**: Image-based product similarity
- **Seasonal Recommendations**: Time-based recommendation adjustments
- **Social Recommendations**: Friend-based recommendations
- **Voice Search Integration**: Voice-activated product search

### Scalability Improvements
- **Microservices Architecture**: Separate recommendation service
- **Message Queues**: Asynchronous recommendation generation
- **CDN Integration**: Cached recommendations globally
- **Machine Learning Pipeline**: Automated model training and deployment

## Troubleshooting

### Common Issues

1. **No Recommendations**: Check user behavior data and product availability
2. **Slow Performance**: Verify database indexes and cache configuration
3. **Low Accuracy**: Review algorithm weights and user profile data
4. **Memory Issues**: Check cache size limits and cleanup policies

### Debug Mode
Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "AccessoryWorld.Services.AI": "Debug"
    }
  }
}
```

## Support

For technical support or feature requests, please contact the development team or create an issue in the project repository.

---

**Version**: 1.0.0  
**Last Updated**: December 2024  
**Compatibility**: .NET 8.0, Entity Framework Core 8.0
