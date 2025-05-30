/**
 * TestPlatform - Professional Light Theme JavaScript
 * Modern, interactive UI enhancements with subtle animations
 */

// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function() {
    // ===== Navbar Scroll Effect =====
    const navbar = document.querySelector('.navbar');

    if (navbar) {
        window.addEventListener('scroll', function() {
            if (window.scrollY > 10) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });

        // Initial check in case page is loaded scrolled down
        if (window.scrollY > 10) {
            navbar.classList.add('scrolled');
        }
    }

    // ===== Card Animations =====
    const cards = document.querySelectorAll('.card');

    // Add staggered animation delay to cards
    cards.forEach((card, index) => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(10px)'; // Reduced from 20px for subtlety
        card.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
        card.style.transitionDelay = `${index * 0.05}s`; // Reduced from 0.1s for quicker load

        setTimeout(() => {
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 50); // Reduced from 100ms for quicker load
    });

    // ===== Form Field Animations =====
    const formFields = document.querySelectorAll('.form-control, .form-select, .form-check, .form-control-modern');

    formFields.forEach((field, index) => {
        field.classList.add('form-animate');
        field.style.transitionDelay = `${index * 0.03}s`; // Reduced for subtlety
    });

    // ===== Interactive Table Rows =====
    const tableRows = document.querySelectorAll('tbody tr');

    tableRows.forEach(row => {
        row.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)'; // Reduced from -3px for subtlety
            this.style.boxShadow = '0 2px 5px rgba(0, 0, 0, 0.05)'; // Lighter shadow
            this.style.zIndex = '1';
        });

        row.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = 'none';
            this.style.zIndex = '0';
        });
    });

    // ===== Button Hover Effects =====
    const buttons = document.querySelectorAll('.btn');

    buttons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            const icon = this.querySelector('i');
            if (icon) {
                icon.style.transform = 'translateX(2px)'; // Reduced from 3px for subtlety
                icon.style.transition = 'transform 0.2s ease';
            }
        });

        button.addEventListener('mouseleave', function() {
            const icon = this.querySelector('i');
            if (icon) {
                icon.style.transform = 'translateX(0)';
            }
        });
    });

    // ===== Enhanced List Items =====
    const listItems = document.querySelectorAll('.list-group-item');

    listItems.forEach(item => {
        item.addEventListener('mouseenter', function() {
            this.style.paddingLeft = 'calc(1.25rem + 3px)'; // Reduced from 5px
        });

        item.addEventListener('mouseleave', function() {
            this.style.paddingLeft = '1.25rem';
        });
    });

    // ===== Question Management =====
    const questionItems = document.querySelectorAll('.question-list-item');

    questionItems.forEach(item => {
        item.addEventListener('mouseenter', function() {
            this.style.borderLeftWidth = '4px'; // Reduced from 5px
        });

        item.addEventListener('mouseleave', function() {
            this.style.borderLeftWidth = '3px';
        });
    });

    // ===== Toast Notifications =====
    // Function to show toast notifications
    window.showToast = function(message, type = 'success') {
        // Create toast container if it doesn't exist
        let toastContainer = document.getElementById('toast-container');

        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toast-container';
            toastContainer.style.position = 'fixed';
            toastContainer.style.top = '80px';
            toastContainer.style.right = '20px';
            toastContainer.style.zIndex = '9999';
            toastContainer.style.maxWidth = '400px';
            document.body.appendChild(toastContainer);
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.classList.add('toast-modern');
        toast.style.minWidth = '300px';

        // Set styles and colors based on type
        let backgroundColor, iconClass;
        switch(type) {
            case 'success':
                backgroundColor = 'rgba(22, 163, 74, 0.95)';
                iconClass = 'fa-check-circle';
                break;
            case 'error':
                backgroundColor = 'rgba(220, 38, 38, 0.95)';
                iconClass = 'fa-exclamation-circle';
                break;
            case 'warning':
                backgroundColor = 'rgba(245, 158, 11, 0.95)';
                iconClass = 'fa-exclamation-triangle';
                break;
            default:
                backgroundColor = 'rgba(14, 165, 233, 0.95)';
                iconClass = 'fa-info-circle';
        }

        toast.style.backgroundColor = backgroundColor;
        toast.style.color = '#fff';
        toast.style.padding = '1rem 1.25rem';
        toast.style.borderRadius = '12px';
        toast.style.marginBottom = '12px';
        toast.style.boxShadow = '0 10px 25px rgba(0, 0, 0, 0.15)';
        toast.style.transform = 'translateX(100%)';
        toast.style.opacity = '0';
        toast.style.transition = 'all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275)';
        toast.style.backdropFilter = 'blur(10px)';
        toast.style.border = '1px solid rgba(255, 255, 255, 0.1)';

        // Create toast content
        const toastContent = document.createElement('div');
        toastContent.style.display = 'flex';
        toastContent.style.alignItems = 'flex-start';
        toastContent.style.gap = '0.75rem';

        // Add icon
        const icon = document.createElement('i');
        icon.classList.add('fas', iconClass);
        icon.style.fontSize = '1.25rem';
        icon.style.marginTop = '0.125rem';
        icon.style.flexShrink = '0';

        // Add message
        const messageDiv = document.createElement('div');
        messageDiv.style.fontSize = '0.9rem';
        messageDiv.style.lineHeight = '1.4';
        messageDiv.style.fontWeight = '500';
        messageDiv.textContent = message;

        // Add close button
        const closeBtn = document.createElement('button');
        closeBtn.style.background = 'none';
        closeBtn.style.border = 'none';
        closeBtn.style.color = 'white';
        closeBtn.style.fontSize = '1.125rem';
        closeBtn.style.cursor = 'pointer';
        closeBtn.style.opacity = '0.8';
        closeBtn.style.padding = '0';
        closeBtn.style.marginLeft = 'auto';
        closeBtn.style.flexShrink = '0';
        closeBtn.innerHTML = '&times;';

        closeBtn.addEventListener('click', () => {
            removeToast(toast);
        });

        closeBtn.addEventListener('mouseenter', () => {
            closeBtn.style.opacity = '1';
        });

        closeBtn.addEventListener('mouseleave', () => {
            closeBtn.style.opacity = '0.8';
        });

        toastContent.appendChild(icon);
        toastContent.appendChild(messageDiv);
        toastContent.appendChild(closeBtn);
        toast.appendChild(toastContent);

        // Add to container
        toastContainer.appendChild(toast);

        // Animate in
        setTimeout(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        }, 10);

        // Auto remove after delay
        const autoRemoveTimeout = setTimeout(() => {
            removeToast(toast);
        }, 5000);

        // Clear timeout if manually closed
        closeBtn.addEventListener('click', () => {
            clearTimeout(autoRemoveTimeout);
        });

        function removeToast(toastElement) {
            toastElement.style.transform = 'translateX(100%)';
            toastElement.style.opacity = '0';

            setTimeout(() => {
                if (toastElement.parentNode) {
                    toastContainer.removeChild(toastElement);
                }
            }, 400);
        }
    };

    // ===== Enhance Success/Error Messages =====
    // Check for success/error messages in TempData
    const successMessage = document.querySelector('.alert-success');
    const errorMessage = document.querySelector('.alert-danger');

    if (successMessage) {
        const messageText = successMessage.textContent.trim();
        if (messageText) {
            showToast(messageText, 'success');
            successMessage.remove(); // Remove the original alert
        }
    }

    if (errorMessage) {
        const messageText = errorMessage.textContent.trim();
        if (messageText) {
            showToast(messageText, 'error');
            errorMessage.remove(); // Remove the original alert
        }
    }

    // ===== Test Lock Toggle Enhancement =====
    const lockTestButton = document.getElementById('lockTestButton');

    if (lockTestButton) {
        lockTestButton.addEventListener('click', function() {
            // Add visual feedback while request is processing
            this.classList.add('disabled');
            const originalText = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Processing...';

            // Original click handler will still work
            // This just adds visual feedback
            setTimeout(() => {
                this.classList.remove('disabled');
                this.innerHTML = originalText;
            }, 800); // Reduced from 1000ms
        });
    }

    // ===== Current Time Display =====
    const timeDisplay = document.getElementById('current-time');

    if (timeDisplay) {
        function updateTime() {
            const now = new Date();
            const hours = now.getHours().toString().padStart(2, '0');
            const minutes = now.getMinutes().toString().padStart(2, '0');
            timeDisplay.textContent = `${hours}:${minutes}`;
        }

        updateTime();
        setInterval(updateTime, 60000); // Update every minute
    }

    // ===== Test Timer Enhancement =====
    const timerElement = document.getElementById('timer');

    if (timerElement) {
        // Add visual effects when time is running low
        const originalTimerUpdate = window.updateTimer;

        if (typeof originalTimerUpdate === 'function') {
            window.updateTimer = function() {
                originalTimerUpdate();

                const timerText = timerElement.textContent;
                const minutes = parseInt(timerText.split(':')[0]);

                // When less than 5 minutes remaining
                if (minutes < 5) {
                    timerElement.style.color = '#f59e0b'; // amber
                }

                // When less than 2 minutes remaining
                if (minutes < 2) {
                    timerElement.style.color = '#dc2626'; // red
                    timerElement.classList.add('animate-pulse');
                }
            };
        }
    }

    // ===== Email Field Management =====
    const addEmailButton = document.getElementById('addSingleField');

    if (addEmailButton) {
        // Enhance the email field addition with animation
        const originalAddSingleField = window.addSingleField;

        if (typeof originalAddSingleField === 'function') {
            window.addSingleField = function() {
                originalAddSingleField();

                // Select the last added email field
                const emailFields = document.querySelectorAll('.email-input-group');
                const lastField = emailFields[emailFields.length - 1];

                if (lastField) {
                    // Apply animation
                    lastField.style.opacity = '0';
                    lastField.style.transform = 'translateX(-10px)'; // Reduced from -20px

                    setTimeout(() => {
                        lastField.style.transition = 'all 0.3s ease';
                        lastField.style.opacity = '1';
                        lastField.style.transform = 'translateX(0)';
                    }, 10);
                }
            };
        }
    }

    // ===== Modal Fixes and Enhancements =====
    // Fix modal backdrop click issues and add modern effects
    document.querySelectorAll('[data-bs-toggle="modal"]').forEach(button => {
        button.addEventListener('click', function() {
            // Get target modal ID
            const targetId = this.getAttribute('data-bs-target');

            // Wait for modal to be shown
            setTimeout(() => {
                // Find the modal-dialog element inside the modal
                const modal = document.querySelector(targetId);
                if (modal) {
                    const modalDialog = modal.querySelector('.modal-dialog');
                    if (modalDialog) {
                        // Make sure the dialog can be interacted with
                        modalDialog.style.pointerEvents = 'auto';

                        // Add entrance animation
                        modalDialog.style.transform = 'scale(0.95) translateY(-10px)';
                        modalDialog.style.opacity = '0';
                        modalDialog.style.transition = 'all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275)';

                        setTimeout(() => {
                            modalDialog.style.transform = 'scale(1) translateY(0)';
                            modalDialog.style.opacity = '1';
                        }, 10);
                    }
                    // Ensure modal has proper z-index
                    modal.style.zIndex = '1055';
                }

                // Make sure backdrops are proper z-index
                document.querySelectorAll('.modal-backdrop').forEach(backdrop => {
                    backdrop.style.zIndex = '1050';
                });
            }, 50);
        });
    });

    // ===== Modern Form Input Effects =====
    // Add floating label effect for modern forms
    document.querySelectorAll('.form-control-modern').forEach(input => {
        // Focus effects
        input.addEventListener('focus', function() {
            this.parentElement.classList.add('focused');

            // Add subtle glow effect
            this.style.boxShadow = '0 0 0 3px rgba(37, 99, 235, 0.1)';
        });

        input.addEventListener('blur', function() {
            this.parentElement.classList.remove('focused');

            // Remove glow effect
            if (!this.classList.contains('is-invalid')) {
                this.style.boxShadow = '';
            }
        });

        // Real-time validation feedback
        input.addEventListener('input', function() {
            if (this.classList.contains('is-invalid') && this.value.trim()) {
                this.classList.remove('is-invalid');
                const feedback = this.nextElementSibling;
                if (feedback && feedback.classList.contains('invalid-feedback')) {
                    feedback.textContent = '';
                }
            }
        });
    });

    // ===== Enhanced Checkbox Interactions =====
    document.querySelectorAll('.form-check-modern').forEach(checkContainer => {
        const checkbox = checkContainer.querySelector('.form-check-input-modern');

        if (checkbox) {
            // Add ripple effect on click
            checkContainer.addEventListener('click', function(e) {
                if (e.target === checkbox) return; // Don't double-trigger

                // Create ripple effect
                const ripple = document.createElement('div');
                ripple.style.position = 'absolute';
                ripple.style.borderRadius = '50%';
                ripple.style.background = 'rgba(37, 99, 235, 0.3)';
                ripple.style.transform = 'scale(0)';
                ripple.style.animation = 'ripple 0.6s linear';
                ripple.style.left = '1rem';
                ripple.style.top = '1rem';
                ripple.style.width = '20px';
                ripple.style.height = '20px';
                ripple.style.pointerEvents = 'none';

                this.style.position = 'relative';
                this.appendChild(ripple);

                setTimeout(() => {
                    ripple.remove();
                }, 600);

                // Toggle checkbox
                checkbox.checked = !checkbox.checked;
                checkbox.dispatchEvent(new Event('change'));
            });
        }
    });

    // ===== Add CSS for ripple animation =====
    if (!document.querySelector('#dynamic-styles')) {
        const style = document.createElement('style');
        style.id = 'dynamic-styles';
        style.textContent = `
            @keyframes ripple {
                to {
                    transform: scale(4);
                    opacity: 0;
                }
            }

            .form-control-modern:focus {
                transform: translateY(-1px);
            }

            .btn-modern:active {
                transform: translateY(0) !important;
            }

            .toast-modern {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            }

            .loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.5);
                display: flex;
                align-items: center;
                justify-content: center;
                z-index: 9999;
                backdrop-filter: blur(4px);
            }

            .loading-spinner {
                background: white;
                padding: 2rem;
                border-radius: 12px;
                text-align: center;
                box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
            }
        `;
        document.head.appendChild(style);
    }

    // ===== Accessibility Improvements =====
    // Add keyboard navigation for custom elements
    document.querySelectorAll('.form-check-modern').forEach(checkContainer => {
        checkContainer.setAttribute('tabindex', '0');
        checkContainer.setAttribute('role', 'checkbox');

        checkContainer.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.click();
            }
        });
    });

    // ===== Performance Optimization =====
    // Debounce resize events
    let resizeTimeout;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            // Trigger any resize-dependent calculations here
            console.log('Window resized');
        }, 250);
    });
});