document.addEventListener('DOMContentLoaded', function () {
    // Get the help icon and tooltip
    const loginHelpIcon = document.getElementById('loginHelpIcon');
    const loginTooltip = document.getElementById('loginTooltip');

    // Show tooltip when hovering over the help icon
    if (loginHelpIcon && loginTooltip) {
        loginHelpIcon.addEventListener('mouseenter', function () {
            loginTooltip.style.display = 'block';
        });

        loginHelpIcon.addEventListener('mouseleave', function () {
            loginTooltip.style.display = 'none';
        });
    }

    // Slider functionality
    const amountSlider = document.getElementById('amountSlider');
    const amountInput = document.querySelector('input[type="number"]');

    if (amountSlider && amountInput) {
        // Update input when slider changes
        amountSlider.addEventListener('input', function () {
            amountInput.value = this.value;
        });

        // Update slider when input changes
        amountInput.addEventListener('input', function () {
            amountSlider.value = this.value;
        });
    }

    // Amount buttons
    const amountButtons = document.querySelectorAll('.amount-btn');

    amountButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Extract the number from the button text (e.g., "+ 100 ₽" -> 100)
            const amount = parseInt(this.textContent.match(/\d+/)[0]);

            // Add to current amount
            amountInput.value = parseInt(amountInput.value) + amount;

            // Update slider
            if (amountSlider) {
                amountSlider.value = amountInput.value;
            }
        });
    });
});