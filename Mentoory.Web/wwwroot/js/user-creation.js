document.addEventListener('DOMContentLoaded', function () {
    const countrySelect = document.getElementById('countrySelect');
    const identificationInput = document.getElementById('identificationInput');
    const identificationLabel = document.getElementById('identificationLabel');
    const skipEmailVerification = document.getElementById('skipEmailVerification');
    const skipInvitationAcceptance = document.getElementById('skipInvitationAcceptance');
    const toggleInfo = document.getElementById('toggleInfo');
    const toggleInfoText = document.getElementById('toggleInfoText');

    // Country-dependent identification mask
    if (countrySelect && identificationInput) {
        countrySelect.addEventListener('change', function () {
            const selected = countrySelect.options[countrySelect.selectedIndex];
            const mask = selected.getAttribute('data-mask');
            const maxLength = selected.getAttribute('data-maxlength');
            const label = selected.getAttribute('data-label');

            if (label && identificationLabel) {
                identificationLabel.textContent = label;
            } else if (identificationLabel) {
                identificationLabel.textContent = 'Identificación';
            }

            if (maxLength) {
                identificationInput.setAttribute('maxlength', maxLength);
            } else {
                identificationInput.removeAttribute('maxlength');
            }

            if (mask) {
                identificationInput.setAttribute('placeholder', mask);
            } else {
                identificationInput.removeAttribute('placeholder');
            }

            identificationInput.value = '';
            identificationInput.focus();
        });

        identificationInput.addEventListener('input', function () {
            const selected = countrySelect.options[countrySelect.selectedIndex];
            const mask = selected.getAttribute('data-mask');
            if (!mask) return;

            const digits = identificationInput.value.replace(/\D/g, '');
            var formatted = '';
            var digitIndex = 0;

            for (var i = 0; i < mask.length && digitIndex < digits.length; i++) {
                if (mask[i] === '0') {
                    formatted += digits[digitIndex++];
                } else {
                    formatted += mask[i];
                    if (digits[digitIndex] === mask[i]) {
                        digitIndex++;
                    }
                }
            }

            identificationInput.value = formatted;
        });
    }

    // Toggle switch feedback
    if (skipEmailVerification && skipInvitationAcceptance && toggleInfo && toggleInfoText) {
        function updateToggleInfo() {
            var skipEmail = skipEmailVerification.checked;
            var skipInvitation = skipInvitationAcceptance.checked;

            if (skipEmail && skipInvitation) {
                toggleInfoText.textContent = 'El usuario será creado como activo con una contraseña temporal. No se enviarán correos.';
                toggleInfo.classList.remove('d-none');
            } else if (!skipEmail && !skipInvitation) {
                toggleInfoText.textContent = 'El usuario recibirá un correo de verificación y luego una invitación al proyecto.';
                toggleInfo.classList.remove('d-none');
            } else if (skipEmail && !skipInvitation) {
                toggleInfoText.textContent = 'El usuario será verificado automáticamente y recibirá una invitación al proyecto.';
                toggleInfo.classList.remove('d-none');
            } else {
                toggleInfoText.textContent = 'El usuario recibirá un correo de verificación y será inscrito automáticamente tras verificar.';
                toggleInfo.classList.remove('d-none');
            }
        }

        skipEmailVerification.addEventListener('change', updateToggleInfo);
        skipInvitationAcceptance.addEventListener('change', updateToggleInfo);

        updateToggleInfo();
    }
});
