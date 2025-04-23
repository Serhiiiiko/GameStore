document.addEventListener('DOMContentLoaded', function () {
    // Get references to the form elements
    const amountSlider = document.querySelector('input[type="range"]');
    const amountInput = document.querySelector('input[type="number"]');
    const paymentButtons = document.querySelectorAll('.payment-options button');

    if (amountSlider && amountInput) {
        // Update input when slider changes
        amountSlider.addEventListener('input', function () {
            amountInput.value = this.value;
        });

        // Update slider when input changes
        amountInput.addEventListener('input', function () {
            // Ensure value is within min/max bounds
            const value = parseInt(this.value);
            const min = parseInt(amountSlider.min);
            const max = parseInt(amountSlider.max);

            if (value < min) {
                this.value = min;
            } else if (value > max) {
                this.value = max;
            }

            amountSlider.value = this.value;
        });
    }

    // Add functionality to payment buttons
    if (paymentButtons.length > 0) {
        paymentButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault(); // Prevent form submission

                // Extract the amount from button text (e.g., "+ 100 ₽" -> 100)
                const amountText = this.textContent.trim();
                const match = amountText.match(/\+ (\d+) ₽/);

                if (match && match[1]) {
                    const amount = parseInt(match[1]);

                    // Add to current amount
                    let currentAmount = parseInt(amountInput.value) || 0;
                    let newAmount = currentAmount + amount;

                    // Ensure it doesn't exceed maximum
                    const max = parseInt(amountSlider.max);
                    if (newAmount > max) {
                        newAmount = max;
                    }

                    // Update input and slider
                    amountInput.value = newAmount;
                    amountSlider.value = newAmount;
                }
            });
        });
    }

    // The tooltip for Steam help icon is already handled via CSS :hover
    // No additional JavaScript needed for that functionality
});