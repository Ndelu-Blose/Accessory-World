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
