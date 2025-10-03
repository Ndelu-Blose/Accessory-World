/**
 * AI Recommendations Service
 * Handles AI-powered product recommendations
 */
class AIRecommendationsService {
    constructor() {
        this.baseUrl = '/api/ai/recommendations';
        this.cache = new Map();
        this.cacheTimeout = 5 * 60 * 1000; // 5 minutes
    }

    /**
     * Get personalized recommendations for a user
     * @param {Object} options - Recommendation options
     * @returns {Promise<Object>} Recommendation response
     */
    async getRecommendations(options = {}) {
        const {
            count = 6,
            algorithmType = 'HYBRID',
            excludeProductIds = [],
            preferredCategories = [],
            preferredBrands = [],
            maxPrice = null,
            minPrice = null,
            includeOutOfStock = false
        } = options;

        const cacheKey = this.generateCacheKey('recommendations', options);
        
        // Check cache first
        if (this.cache.has(cacheKey)) {
            const cached = this.cache.get(cacheKey);
            if (Date.now() - cached.timestamp < this.cacheTimeout) {
                return cached.data;
            }
        }

        try {
            const params = new URLSearchParams({
                count: count.toString(),
                algorithmType,
                includeOutOfStock: includeOutOfStock.toString()
            });

            if (excludeProductIds.length > 0) {
                params.append('excludeProductIds', excludeProductIds.join(','));
            }
            if (preferredCategories.length > 0) {
                params.append('preferredCategories', preferredCategories.join(','));
            }
            if (preferredBrands.length > 0) {
                params.append('preferredBrands', preferredBrands.join(','));
            }
            if (maxPrice !== null) {
                params.append('maxPrice', maxPrice.toString());
            }
            if (minPrice !== null) {
                params.append('minPrice', minPrice.toString());
            }

            const response = await fetch(`${this.baseUrl}?${params}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            // Cache the result
            this.cache.set(cacheKey, {
                data,
                timestamp: Date.now()
            });

            return data;
        } catch (error) {
            console.error('Error fetching recommendations:', error);
            return { products: [], algorithmUsed: 'FALLBACK', processingTimeMs: 0 };
        }
    }

    /**
     * Get similar products for a given product
     * @param {number} productId - Product ID
     * @param {Object} options - Similarity options
     * @returns {Promise<Object>} Similar products response
     */
    async getSimilarProducts(productId, options = {}) {
        const {
            count = 4,
            similarityType = 'CONTENT',
            excludeProductIds = []
        } = options;

        const cacheKey = this.generateCacheKey('similar', { productId, ...options });
        
        // Check cache first
        if (this.cache.has(cacheKey)) {
            const cached = this.cache.get(cacheKey);
            if (Date.now() - cached.timestamp < this.cacheTimeout) {
                return cached.data;
            }
        }

        try {
            const params = new URLSearchParams({
                productId: productId.toString(),
                count: count.toString(),
                similarityType
            });

            if (excludeProductIds.length > 0) {
                params.append('excludeProductIds', excludeProductIds.join(','));
            }

            const response = await fetch(`${this.baseUrl}/similar?${params}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            // Cache the result
            this.cache.set(cacheKey, {
                data,
                timestamp: Date.now()
            });

            return data;
        } catch (error) {
            console.error('Error fetching similar products:', error);
            return { products: [], similarityType: 'FALLBACK', processingTimeMs: 0 };
        }
    }

    /**
     * Track user behavior for recommendation learning
     * @param {Object} behaviorData - Behavior data
     */
    async trackBehavior(behaviorData) {
        try {
            const response = await fetch(`${this.baseUrl}/behavior`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    ...behaviorData,
                    sessionId: this.getSessionId(),
                    deviceType: this.getDeviceType(),
                    userAgent: navigator.userAgent,
                    timestamp: new Date().toISOString()
                })
            });

            if (!response.ok) {
                console.warn('Failed to track behavior:', response.status);
            }
        } catch (error) {
            console.error('Error tracking behavior:', error);
        }
    }

    /**
     * Record feedback on recommendations
     * @param {number} recommendationId - Recommendation ID
     * @param {string} feedbackType - Type of feedback
     * @param {string} comment - Optional comment
     */
    async recordFeedback(recommendationId, feedbackType, comment = null) {
        try {
            const response = await fetch(`${this.baseUrl}/feedback`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    recommendationId,
                    feedbackType,
                    comment
                })
            });

            if (!response.ok) {
                console.warn('Failed to record feedback:', response.status);
            }
        } catch (error) {
            console.error('Error recording feedback:', error);
        }
    }

    /**
     * Get user profile for recommendations
     * @returns {Promise<Object>} User profile
     */
    async getUserProfile() {
        try {
            const response = await fetch(`${this.baseUrl}/profile`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error fetching user profile:', error);
            return null;
        }
    }

    /**
     * Update user profile for recommendations
     * @param {Object} profileData - Profile data
     */
    async updateUserProfile(profileData) {
        try {
            const response = await fetch(`${this.baseUrl}/profile`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(profileData)
            });

            if (!response.ok) {
                console.warn('Failed to update user profile:', response.status);
            }
        } catch (error) {
            console.error('Error updating user profile:', error);
        }
    }

    /**
     * Assign user to A/B test group
     * @param {string} testName - Test name
     * @returns {Promise<Object>} A/B test assignment
     */
    async assignToTestGroup(testName) {
        try {
            const response = await fetch(`${this.baseUrl}/ab-test`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({
                    testName,
                    description: `A/B test for ${testName}`,
                    variants: ['A', 'B'],
                    trafficAllocation: 1.0,
                    startDate: new Date().toISOString(),
                    targetUserSegments: []
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Error assigning to test group:', error);
            return { testId: 1, testName, assignedVariant: 'A', isActive: true };
        }
    }

    /**
     * Generate cache key for recommendations
     * @param {string} type - Cache type
     * @param {Object} options - Options
     * @returns {string} Cache key
     */
    generateCacheKey(type, options) {
        return `${type}_${JSON.stringify(options)}`;
    }

    /**
     * Get anti-forgery token
     * @returns {string} Anti-forgery token
     */
    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    /**
     * Get session ID
     * @returns {string} Session ID
     */
    getSessionId() {
        const meta = document.querySelector('meta[name="session-id"]');
        return meta ? meta.content : Math.random().toString(36).substring(2, 15);
    }

    /**
     * Get device type based on screen width
     * @returns {string} Device type
     */
    getDeviceType() {
        const width = window.innerWidth;
        if (width < 768) return 'MOBILE';
        if (width < 1024) return 'TABLET';
        return 'DESKTOP';
    }

    /**
     * Clear cache
     */
    clearCache() {
        this.cache.clear();
    }

    /**
     * Get cache statistics
     * @returns {Object} Cache statistics
     */
    getCacheStats() {
        return {
            size: this.cache.size,
            keys: Array.from(this.cache.keys())
        };
    }
}

// Global instance
window.AIRecommendations = new AIRecommendationsService();

// Auto-track page views
document.addEventListener('DOMContentLoaded', function() {
    // Track page view
    const currentPath = window.location.pathname;
    const productId = extractProductIdFromUrl(currentPath);
    
    if (productId) {
        window.AIRecommendations.trackBehavior({
            productId: productId,
            actionType: 'VIEW',
            category: getProductCategory(),
            brand: getProductBrand(),
            price: getProductPrice()
        });
    }
});

/**
 * Extract product ID from URL
 * @param {string} path - URL path
 * @returns {number|null} Product ID
 */
function extractProductIdFromUrl(path) {
    const match = path.match(/\/Products\/Details\/(\d+)/);
    return match ? parseInt(match[1]) : null;
}

/**
 * Get product category from page
 * @returns {string|null} Product category
 */
function getProductCategory() {
    const categoryElement = document.querySelector('[data-product-category]');
    return categoryElement ? categoryElement.dataset.productCategory : null;
}

/**
 * Get product brand from page
 * @returns {string|null} Product brand
 */
function getProductBrand() {
    const brandElement = document.querySelector('[data-product-brand]');
    return brandElement ? brandElement.dataset.productBrand : null;
}

/**
 * Get product price from page
 * @returns {number|null} Product price
 */
function getProductPrice() {
    const priceElement = document.querySelector('[data-product-price]');
    return priceElement ? parseFloat(priceElement.dataset.productPrice) : null;
}
