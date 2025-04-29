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
    const formFields = document.querySelectorAll('.form-control, .form-select, .form-check');

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
            document.body.appendChild(toastContainer);
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.classList.add('toast');
        toast.style.minWidth = '250px';

        // Set background color based on type
        switch(type) {
            case 'success':
                toast.style.backgroundColor = 'rgba(22, 163, 74, 0.9)';
                break;
            case 'error':
                toast.style.backgroundColor = 'rgba(220, 38, 38, 0.9)';
                break;
            case 'warning':
                toast.style.backgroundColor = 'rgba(245, 158, 11, 0.9)';
                break;
            default:
                toast.style.backgroundColor = 'rgba(14, 165, 233, 0.9)';
        }

        toast.style.color = '#fff';
        toast.style.padding = '0.75rem 1rem';
        toast.style.borderRadius = '6px';
        toast.style.marginBottom = '10px';
        toast.style.boxShadow = '0 3px 10px rgba(0, 0, 0, 0.1)';
        toast.style.transform = 'translateX(100%)';
        toast.style.opacity = '0';
        toast.style.transition = 'all 0.3s ease';

        // Add icon based on type
        const icon = document.createElement('i');
        icon.classList.add('fas');
        icon.classList.add(
            type === 'success' ? 'fa-check-circle' :
                type === 'error' ? 'fa-exclamation-circle' :
                    type === 'warning' ? 'fa-exclamation-triangle' : 'fa-info-circle'
        );
        icon.style.marginRight = '8px';

        toast.appendChild(icon);
        toast.appendChild(document.createTextNode(message));

        // Add to container
        toastContainer.appendChild(toast);

        // Animate in
        setTimeout(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        }, 10);

        // Remove after delay
        setTimeout(() => {
            toast.style.transform = 'translateX(100%)';
            toast.style.opacity = '0';

            setTimeout(() => {
                if (toast.parentNode) {
                    toastContainer.removeChild(toast);
                }
            }, 300);
        }, 4000);
    };

    // ===== Enhance Success/Error Messages =====
    // Check for success/error messages in TempData
    const successMessage = document.querySelector('.alert-success');
    const errorMessage = document.querySelector('.alert-danger');

    if (successMessage) {
        showToast(successMessage.textContent, 'success');
        successMessage.remove(); // Remove the original alert
    }

    if (errorMessage) {
        showToast(errorMessage.textContent, 'error');
        errorMessage.remove(); // Remove the original alert
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

    // ===== Modal Fixes =====
    // Fix modal backdrop click issues
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
});