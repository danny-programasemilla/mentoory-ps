document.addEventListener('DOMContentLoaded', function () {
    var countrySelect = document.getElementById('countrySelect');
    var identificationInput = document.getElementById('identificationInput');
    var identificationLabel = document.getElementById('identificationLabel');
    var skipEmailVerification = document.getElementById('skipEmailVerification');
    var skipInvitationAcceptance = document.getElementById('skipInvitationAcceptance');
    var toggleInfo = document.getElementById('toggleInfo');
    var toggleInfoText = document.getElementById('toggleInfoText');

    // Country-dependent identification mask
    if (countrySelect && identificationInput) {
        countrySelect.addEventListener('change', function () {
            var selected = countrySelect.options[countrySelect.selectedIndex];
            var mask = selected.getAttribute('data-mask');
            var maxLength = selected.getAttribute('data-maxlength');
            var label = selected.getAttribute('data-label');

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
            var selected = countrySelect.options[countrySelect.selectedIndex];
            var mask = selected.getAttribute('data-mask');
            if (!mask) return;

            var digits = identificationInput.value.replace(/\D/g, '');
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
            } else if (!skipEmail && !skipInvitation) {
                toggleInfoText.textContent = 'El usuario recibirá un correo de verificación y luego una invitación al proyecto.';
            } else if (skipEmail && !skipInvitation) {
                toggleInfoText.textContent = 'El usuario será verificado automáticamente y recibirá una invitación al proyecto.';
            } else {
                toggleInfoText.textContent = 'El usuario recibirá un correo de verificación y será inscrito automáticamente tras verificar.';
            }

            toggleInfo.classList.remove('d-none');
        }

        skipEmailVerification.addEventListener('change', updateToggleInfo);
        skipInvitationAcceptance.addEventListener('change', updateToggleInfo);

        updateToggleInfo();
    }
});
