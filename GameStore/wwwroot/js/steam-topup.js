document.addEventListener('DOMContentLoaded', function () {
    // Get references to the form elements
    const amountSlider = document.querySelector('input[type="range"]');
    const amountInput = document.querySelector('input[type="number"]');

    // Remove any existing event listeners by cloning and replacing elements
    const paymentButtons = document.querySelectorAll('.payment-options button');
    paymentButtons.forEach(button => {
        const newButton = button.cloneNode(true);
        button.parentNode.replaceChild(newButton, button);
    });

    // Get fresh references after replacement
    const freshButtons = document.querySelectorAll('.payment-options button');

    if (amountSlider && amountInput) {
        // Update input when slider changes
        amountSlider.addEventListener('input', function () {
            amountInput.value = this.value;
        });

        // Update slider when input changes
        amountInput.addEventListener('input', function () {
            // Ensure value is within min/max bounds
            let value = parseInt(this.value) || 0;
            const min = parseInt(amountSlider.min);
            const max = parseInt(amountSlider.max);

            if (value < min) {
                value = min;
                this.value = min;
            } else if (value > max) {
                value = max;
                this.value = max;
            }

            amountSlider.value = value;
        });
    }

    // Fixed event handlers for payment buttons
    if (freshButtons.length > 0) {
        freshButtons.forEach(button => {
            button.addEventListener('click', function (e) {
                e.preventDefault();
                console.log("Button clicked");

                // Extract the amount from button text (e.g., "+ 100 ₽" -> 100)
                const amountText = this.textContent.trim();
                const match = amountText.match(/\+ (\d+) ₽/);

                if (match && match[1]) {
                    // Parse the exact increment amount from the button text
                    const incrementAmount = parseInt(match[1]);
                    console.log("Increment amount:", incrementAmount);

                    // Get current value directly from input
                    let currentAmount = parseInt(amountInput.value) || 0;
                    console.log("Current amount:", currentAmount);

                    // Calculate new amount
                    let newAmount = currentAmount + incrementAmount;
                    console.log("New amount:", newAmount);

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
});