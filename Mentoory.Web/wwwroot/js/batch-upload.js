document.addEventListener('DOMContentLoaded', function () {
    const fileInput = document.querySelector('input[type="file"]');
    if (!fileInput) return;

    fileInput.addEventListener('change', function () {
        const file = fileInput.files[0];
        if (!file) return;

        if (!file.name.toLowerCase().endsWith('.csv')) {
            alert('Solo se permiten archivos CSV.');
            fileInput.value = '';
            return;
        }

        // Check approximate row count (file size heuristic: ~100 bytes per row)
        if (file.size > 500 * 200) {
            alert('El archivo parece tener más de 500 filas. El máximo permitido es 500.');
            fileInput.value = '';
        }
    });
});
