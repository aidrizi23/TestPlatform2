document.addEventListener('DOMContentLoaded', function() {
    // Focus on token input when page loads
    const tokenInput = document.getElementById('token');
    if (tokenInput) {
        tokenInput.focus();
    }

    // Add subtle parallax effect to hero visual
    const heroVisual = document.querySelector('.hero-visual');
    if (heroVisual) {
        window.addEventListener('scroll', function() {
            const scrolled = window.pageYOffset;
            const rate = scrolled * -0.1;
            heroVisual.style.transform = `translateY(${rate}px)`;
        });
    }

    // Animate stats on scroll
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate');
            }
        });
    }, observerOptions);

    // Observe elements for animation
    document.querySelectorAll('.stat-number, .feature-card, .step-item').forEach(el => {
        observer.observe(el);
    });

    // Add hover effects to feature cards
    document.querySelectorAll('.feature-card').forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-8px) scale(1.02)';
        });

        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0) scale(1)';
        });
    });

    // Create Test Modal Functionality - Only if user is authenticated
    if (document.getElementById('createTestForm')) {
        const createTestForm = document.getElementById('createTestForm');
        const createTestModal = new bootstrap.Modal(document.getElementById('createTestModal'));
        const successModal = new bootstrap.Modal(document.getElementById('successModal'));
        const loadingOverlay = document.getElementById('loadingOverlay');

        // Form inputs for live preview
        const titleInput = document.getElementById('testTitle');
        const descriptionInput = document.getElementById('testDescription');
        const timeLimitInput = document.getElementById('timeLimit');
        const maxAttemptsInput = document.getElementById('maxAttempts');
        const randomizeInput = document.getElementById('randomizeQuestions');

        // Preview elements
        const previewTitle = document.getElementById('preview-title');
        const previewTime = document.getElementById('preview-time');
        const previewAttempts = document.getElementById('preview-attempts');
        const previewRandomize = document.getElementById('preview-randomize');
        const previewFeatures = document.getElementById('preview-features');

        // Utility functions
        function showLoading() {
            loadingOverlay.style.display = 'flex';
        }

        function hideLoading() {
            loadingOverlay.style.display = 'none';
        }

        function clearFormErrors() {
            const inputs = createTestForm.querySelectorAll('.form-control');
            const feedbacks = createTestForm.querySelectorAll('.invalid-feedback');

            inputs.forEach(input => {
                input.classList.remove('is-invalid');
            });

            feedbacks.forEach(feedback => {
                feedback.textContent = '';
            });
        }

        function showFormErrors(errors) {
            clearFormErrors();

            errors.forEach(error => {
                const input = createTestForm.querySelector(`[name="${error.Field}"]`);
                const feedback = input ? input.parentElement.querySelector('.invalid-feedback') : null;

                if (input && feedback) {
                    input.classList.add('is-invalid');
                    feedback.textContent = error.Message;
                }
            });
        }

        function showToast(message, type = 'success') {
            // Create toast container if it doesn't exist
            let toastContainer = document.querySelector('.toast-container');
            if (!toastContainer) {
                toastContainer = document.createElement('div');
                toastContainer.className = 'toast-container';
                document.body.appendChild(toastContainer);
            }

            // Create toast element
            const toast = document.createElement('div');
            toast.className = `toast ${type}`;
            toast.innerHTML = `
                <div class="toast-body d-flex align-items-center">
                    <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'exclamation-triangle'} me-2"></i>
                    <span>${message}</span>
                </div>
            `;

            toastContainer.appendChild(toast);

            // Show toast
            setTimeout(() => {
                toast.style.opacity = '1';
                toast.style.transform = 'translateX(0)';
            }, 100);

            // Hide toast after 4 seconds
            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(100%)';
                setTimeout(() => {
                    toast.remove();
                }, 300);
            }, 4000);
        }

        // Live preview updates
        function updatePreview() {
            const title = titleInput.value.trim() || 'Your test title';
            const timeLimit = timeLimitInput.value || '60';
            const maxAttempts = maxAttemptsInput.value || '1';
            const isRandomized = randomizeInput.checked;

            previewTitle.textContent = title;
            previewTime.textContent = `${timeLimit} minutes`;
            previewAttempts.textContent = maxAttempts;
            previewRandomize.textContent = isRandomized ? 'Randomized' : 'Sequential';

            // Update feature tags
            let featuresHTML = '';
            if (isRandomized) {
                featuresHTML += '<span class="feature-tag"><i class="fas fa-random"></i> Randomized Questions</span>';
            }
            if (parseInt(maxAttempts) > 1) {
                featuresHTML += '<span class="feature-tag"><i class="fas fa-redo"></i> Multiple Attempts</span>';
            }
            if (parseInt(timeLimit) >= 120) {
                featuresHTML += '<span class="feature-tag"><i class="fas fa-clock"></i> Extended Time</span>';
            }
            previewFeatures.innerHTML = featuresHTML;
        }

        // Add event listeners for live preview
        titleInput.addEventListener('input', updatePreview);
        timeLimitInput.addEventListener('input', updatePreview);
        maxAttemptsInput.addEventListener('input', updatePreview);
        randomizeInput.addEventListener('change', updatePreview);

        // Form submission
        createTestForm.addEventListener('submit', async function(e) {
            e.preventDefault();

            const submitButton = this.querySelector('.btn-create-test');
            const originalText = submitButton.innerHTML;

            showLoading();
            submitButton.disabled = true;
            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Creating...';
            clearFormErrors();

            try {
                // Get CSRF token
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

                // Create the data object matching TestForCreationDto exactly
                const data = {
                    Title: titleInput.value.trim(),
                    Description: descriptionInput.value.trim(),
                    TimeLimit: parseInt(timeLimitInput.value) || 60,
                    MaxAttempts: parseInt(maxAttemptsInput.value) || 1,
                    RandomizeQuestions: randomizeInput.checked
                };

                const headers = {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                };

                // Add CSRF token if available
                if (token) {
                    headers['RequestVerificationToken'] = token;
                }

                // This URL would need to be dynamically generated or passed to the script
                const createTestUrl = '/Test/CreateAjax'; // This should be made configurable

                const response = await fetch(createTestUrl, {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify(data)
                });

                const result = await response.json();

                if (result.success) {
                    createTestModal.hide();

                    // Update success modal
                    document.getElementById('successMessage').textContent = result.message;
                    document.getElementById('viewTestLink').href = `/Test/Details/${result.testId}`;

                    successModal.show();
                    showToast('Test created successfully!', 'success');
                } else {
                    if (result.errors) {
                        showFormErrors(result.errors);
                    } else {
                        showToast(result.message || 'An error occurred', 'error');
                    }
                }
            } catch (error) {
                console.error('Error creating test:', error);
                showToast('An error occurred while creating the test', 'error');
            } finally {
                hideLoading();
                submitButton.disabled = false;
                submitButton.innerHTML = originalText;
            }
        });

        // Reset form when modal is hidden
        document.getElementById('createTestModal').addEventListener('hidden.bs.modal', function() {
            createTestForm.reset();
            clearFormErrors();
            updatePreview();

            // Focus on title input when modal is shown again
            titleInput.focus();
        });

        // Focus title input when modal is shown
        document.getElementById('createTestModal').addEventListener('shown.bs.modal', function() {
            titleInput.focus();
        });

        // Initialize preview with default values
        updatePreview();
    }
});