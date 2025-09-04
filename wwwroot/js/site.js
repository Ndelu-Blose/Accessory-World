// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Newsletter Form Handler
document.addEventListener('DOMContentLoaded', function() {
    const newsletterForm = document.getElementById('newsletter-form');
    const emailInput = document.getElementById('newsletter-email');
    const submitButton = document.getElementById('newsletter-submit');
    const messageDiv = document.getElementById('newsletter-message');

    if (newsletterForm) {
        newsletterForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const email = emailInput.value.trim();
            
            // Basic email validation
            if (!email) {
                showMessage('Please enter your email address.', 'error');
                return;
            }
            
            if (!isValidEmail(email)) {
                showMessage('Please enter a valid email address.', 'error');
                return;
            }
            
            // Disable form during submission
            submitButton.disabled = true;
            submitButton.textContent = 'Subscribing...';
            
            try {
                const response = await fetch('/api/newsletter/subscribe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ email: email })
                });
                
                const result = await response.json();
                
                if (response.ok) {
                    showMessage(result.message || 'Successfully subscribed to our newsletter!', 'success');
                    emailInput.value = '';
                } else {
                    showMessage(result.message || 'An error occurred. Please try again.', 'error');
                }
            } catch (error) {
                console.error('Newsletter subscription error:', error);
                showMessage('Network error. Please check your connection and try again.', 'error');
            } finally {
                // Re-enable form
                submitButton.disabled = false;
                submitButton.textContent = 'Subscribe';
            }
        });
    }
    
    function isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }
    
    function showMessage(message, type) {
        if (messageDiv) {
            messageDiv.textContent = message;
            messageDiv.className = `newsletter-message ${type}`;
            messageDiv.style.display = 'block';
            
            // Hide message after 5 seconds
            setTimeout(() => {
                messageDiv.style.display = 'none';
            }, 5000);
        }
    }
});

// Cart functionality
function addToCart(productId, skuId = null, quantity = 1) {
    // Get the anti-forgery token from meta tag or input
    let token = document.querySelector('meta[name="RequestVerificationToken"]')?.content;
    if (!token) {
        token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    }
    
    if (!token) {
        console.error('Anti-forgery token not found');
        showCartMessage('Security token missing. Please refresh the page.', 'error');
        return;
    }
    
    // If no SKU ID provided, try to get it from the product card data attribute
    if (!skuId) {
        const productCard = event?.target?.closest('.product-card');
        const skuIdFromCard = productCard?.dataset?.skuId;
        if (skuIdFromCard) {
            skuId = parseInt(skuIdFromCard);
        } else {
            // Fallback: assume SKU ID equals Product ID (based on seeder logic)
            skuId = productId;
        }
    }
    
    const requestData = {
        ProductId: productId,
        SKUId: skuId,
        Quantity: quantity
    };
    
    // Show loading state
    const button = event?.target;
    const originalText = button?.innerHTML;
    if (button) {
        button.disabled = true;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Adding...';
    }
    
    fetch('/Cart/AddItem', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token
        },
        body: JSON.stringify(requestData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            showCartMessage('Item added to cart successfully!', 'success');
            updateCartCount(data.cartCount);
        } else {
            showCartMessage(data.message || 'Failed to add item to cart.', 'error');
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        showCartMessage('An error occurred. Please try again.', 'error');
    })
    .finally(() => {
        // Restore button state
        if (button) {
            button.disabled = false;
            button.innerHTML = originalText;
        }
    });
}

function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll('.cart-count, .badge');
    cartCountElements.forEach(element => {
        element.textContent = count;
        element.style.display = count > 0 ? 'inline' : 'none';
    });
}

function showCartMessage(message, type) {
    // Create or update cart message element
    let messageDiv = document.getElementById('cart-message');
    if (!messageDiv) {
        messageDiv = document.createElement('div');
        messageDiv.id = 'cart-message';
        messageDiv.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            border-radius: 8px;
            z-index: 9999;
            font-weight: 500;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            transition: all 0.3s ease;
        `;
        document.body.appendChild(messageDiv);
    }
    
    messageDiv.textContent = message;
    messageDiv.className = `cart-message ${type}`;
    
    if (type === 'success') {
        messageDiv.style.backgroundColor = '#d4edda';
        messageDiv.style.color = '#155724';
        messageDiv.style.border = '1px solid #c3e6cb';
    } else {
        messageDiv.style.backgroundColor = '#f8d7da';
        messageDiv.style.color = '#721c24';
        messageDiv.style.border = '1px solid #f5c6cb';
    }
    
    messageDiv.style.display = 'block';
    messageDiv.style.opacity = '1';
    
    // Hide message after 4 seconds
    setTimeout(() => {
        messageDiv.style.opacity = '0';
        setTimeout(() => {
            messageDiv.style.display = 'none';
        }, 300);
    }, 4000);
}

// Load cart count on page load
document.addEventListener('DOMContentLoaded', function() {
    fetch('/Cart/GetCount')
        .then(response => response.json())
        .then(data => {
            updateCartCount(data.count);
        })
        .catch(error => {
            console.error('Error loading cart count:', error);
        });
});
