// Landing Page JavaScript - Accessory World

// DOM Content Loaded Event
document.addEventListener('DOMContentLoaded', function() {
    initializeTabSwitching();
    initializeSearch();
    initializeNewsletterForm();
    initializeMobileMenu();
    initializeCartUpdates();
});

// Tab Switching Functionality
function initializeTabSwitching() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const productGrids = {
        'bestsellers': document.getElementById('bestsellers-grid'),
        'laptops': document.getElementById('laptops-grid'),
        'iwatch': document.getElementById('iwatch-grid')
    };

    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all buttons
            tabButtons.forEach(btn => btn.classList.remove('active'));
            
            // Add active class to clicked button
            this.classList.add('active');
            
            // Hide all product grids
            Object.values(productGrids).forEach(grid => {
                if (grid) {
                    grid.classList.add('d-none');
                }
            });
            
            // Show target grid
            if (productGrids[targetTab]) {
                productGrids[targetTab].classList.remove('d-none');
            }
        });
    });
}

// Search Functionality
function initializeSearch() {
    const searchInput = document.querySelector('.search-box input');
    const searchForm = document.querySelector('.search-box');
    
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const query = searchInput.value.trim();
            
            if (query) {
                // Redirect to products page with search query
                window.location.href = `/Products?search=${encodeURIComponent(query)}`;
            }
        });
    }
    
    // Search suggestions (optional enhancement)
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            const query = this.value.trim();
            
            if (query.length >= 2) {
                searchTimeout = setTimeout(() => {
                    // Could implement search suggestions here
                    console.log('Search suggestions for:', query);
                }, 300);
            }
        });
    }
}

// Newsletter Form
function initializeNewsletterForm() {
    const newsletterForm = document.querySelector('.newsletter-form');
    
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const emailInput = this.querySelector('input[type="email"]');
            const email = emailInput.value.trim();
            
            if (email && isValidEmail(email)) {
                // Simulate newsletter subscription
                showNotification('Thank you for subscribing to our newsletter!', 'success');
                emailInput.value = '';
                
                // Here you would typically send the email to your backend
                console.log('Newsletter subscription:', email);
            } else {
                showNotification('Please enter a valid email address.', 'error');
            }
        });
    }
}

// Mobile Menu Toggle
function initializeMobileMenu() {
    const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');
    const navbar = document.querySelector('.navbar');
    
    if (mobileMenuToggle && navbar) {
        mobileMenuToggle.addEventListener('click', function() {
            navbar.classList.toggle('mobile-menu-open');
        });
    }
}

// Cart Updates
function initializeCartUpdates() {
    // Update cart count display
    updateCartCount();
    
    // Listen for cart update events
    document.addEventListener('cartUpdated', function() {
        updateCartCount();
    });
}

// Helper Functions
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function showNotification(message, type = 'info') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    
    // Add styles
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${type === 'success' ? '#10B981' : type === 'error' ? '#EF4444' : '#3B82F6'};
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        z-index: 1000;
        font-weight: 500;
        max-width: 300px;
        word-wrap: break-word;
        animation: slideIn 0.3s ease-out;
    `;
    
    // Add animation keyframes if not already added
    if (!document.querySelector('#notification-styles')) {
        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            @keyframes slideIn {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }
            @keyframes slideOut {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(100%);
                    opacity: 0;
                }
            }
        `;
        document.head.appendChild(style);
    }
    
    // Add to page
    document.body.appendChild(notification);
    
    // Remove after 4 seconds
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-in';
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 300);
    }, 4000);
}

function updateCartCount(count) {
    // Update cart count badge in main navigation
    const cartCountElement = document.getElementById('cart-count');
    if (cartCountElement) {
        cartCountElement.textContent = count;
        cartCountElement.style.display = count > 0 ? 'inline' : 'none';
    }
    
    // Update cart count badge in landing layout
    const cartCountLandingElement = document.getElementById('cart-count-landing');
    if (cartCountLandingElement) {
        cartCountLandingElement.textContent = count;
        cartCountLandingElement.style.display = count > 0 ? 'inline' : 'none';
    }
    
    // Also update any other cart count elements
    const otherCartCountElements = document.querySelectorAll('.cart-count, .badge');
    otherCartCountElements.forEach(element => {
        if (element.id !== 'cart-count' && element.id !== 'cart-count-landing') {
            element.textContent = count;
            element.style.display = count > 0 ? 'inline' : 'none';
        }
    });
}

function getCartItemCount() {
    // Simulate cart item count - replace with actual implementation
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    return cart.reduce((total, item) => total + (item.quantity || 1), 0);
}

function getCartTotal() {
    // Simulate cart total - replace with actual implementation
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    return cart.reduce((total, item) => total + (item.price * (item.quantity || 1)), 0);
}

// Product Card Interactions
function addToCart(productId, skuId) {
    // Simulate adding to cart
    const cart = JSON.parse(localStorage.getItem('cart') || '[]');
    const existingItem = cart.find(item => item.productId === productId && item.skuId === skuId);
    
    if (existingItem) {
        existingItem.quantity += 1;
    } else {
        cart.push({
            productId: productId,
            skuId: skuId,
            quantity: 1,
            price: 0 // This should be fetched from the product data
        });
    }
    
    localStorage.setItem('cart', JSON.stringify(cart));
    
    // Dispatch cart updated event
    document.dispatchEvent(new CustomEvent('cartUpdated'));
    
    showNotification('Product added to cart!', 'success');
}

// Proper API-based add to cart for product cards
async function addToCartFromCard(productId, skuId) {
    try {
        // Get CSRF token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || 
                     document.querySelector('meta[name="__RequestVerificationToken"]')?.content || '';
        
        // Add to cart via API
        const response = await fetch('/Cart/AddItem', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                productId: productId,
                skuId: skuId,
                quantity: 1
            })
        });
        
        if (response.status === 401) {
            // User needs to login
            showNotification('Please login to add items to your cart.', 'info');
            setTimeout(() => {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            }, 2000);
            return;
        }
        
        const data = await response.json();
        
        if (data.success) {
            showNotification('Product added to cart successfully!', 'success');
            updateCartCount(data.cartCount);
        } else {
            showNotification(data.message || 'Failed to add item to cart.', 'error');
        }
        
    } catch (error) {
        console.error('Error adding to cart:', error);
        showNotification('An error occurred while adding to cart.', 'error');
    }
}

// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Lazy loading for images (if needed)
if ('IntersectionObserver' in window) {
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                if (img.dataset.src) {
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    observer.unobserve(img);
                }
            }
        });
    });
    
    document.querySelectorAll('img[data-src]').forEach(img => {
        imageObserver.observe(img);
    });
}

// Export functions for global access
window.LandingPage = {
    addToCart,
    addToCartFromCard,
    showNotification,
    updateCartCount
};

// Make addToCartFromCard globally available
window.addToCartFromCard = addToCartFromCard;