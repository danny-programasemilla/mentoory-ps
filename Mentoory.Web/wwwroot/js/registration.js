document.addEventListener('DOMContentLoaded', function () {
    const countrySelect = document.getElementById('countrySelect');
    const nationalIdInput = document.getElementById('nationalIdInput');
    const nationalIdLabel = document.getElementById('nationalIdLabel');

    if (!countrySelect || !nationalIdInput) return;

    countrySelect.addEventListener('change', function () {
        const selected = countrySelect.options[countrySelect.selectedIndex];
        const mask = selected.getAttribute('data-mask');
        const maxLength = selected.getAttribute('data-maxlength');
        const label = selected.getAttribute('data-label');

        if (label) {
            nationalIdLabel.textContent = label;
        } else {
            nationalIdLabel.textContent = 'Identificación';
        }

        if (maxLength) {
            nationalIdInput.setAttribute('maxlength', maxLength);
        } else {
            nationalIdInput.removeAttribute('maxlength');
        }

        if (mask) {
            nationalIdInput.setAttribute('placeholder', mask);
        } else {
            nationalIdInput.removeAttribute('placeholder');
        }

        nationalIdInput.value = '';
        nationalIdInput.focus();
    });

    // Apply simple mask formatting on input
    nationalIdInput.addEventListener('input', function () {
        const selected = countrySelect.options[countrySelect.selectedIndex];
        const mask = selected.getAttribute('data-mask');
        if (!mask) return;

        const digits = nationalIdInput.value.replace(/\D/g, '');
        let formatted = '';
        let digitIndex = 0;

        for (let i = 0; i < mask.length && digitIndex < digits.length; i++) {
            if (mask[i] === '0') {
                formatted += digits[digitIndex++];
            } else {
                formatted += mask[i];
                if (digits[digitIndex] === mask[i]) {
                    digitIndex++;
                }
            }
        }

        nationalIdInput.value = formatted;
    });
});
