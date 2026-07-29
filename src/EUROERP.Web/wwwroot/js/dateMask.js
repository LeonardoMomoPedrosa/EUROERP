/**
 * Date mask: .date-mask-ddmmyyyy → dd/MM/yyyy (digits only; slashes inserted).
 */
(function () {
    function formatDateMask(value) {
        var digits = (value || '').replace(/\D/g, '');
        if (digits.length > 8) digits = digits.substring(0, 8);
        if (digits.length === 0) return '';
        if (digits.length <= 2) return digits;
        if (digits.length <= 4) return digits.substring(0, 2) + '/' + digits.substring(2);
        return digits.substring(0, 2) + '/' + digits.substring(2, 4) + '/' + digits.substring(4, 8);
    }

    document.addEventListener('input', function (e) {
        if (!e.target.matches || !e.target.matches('.date-mask-ddmmyyyy')) return;
        var el = e.target;
        var start = el.selectionStart;
        var oldLen = el.value.length;
        var formatted = formatDateMask(el.value);
        el.value = formatted;
        var newLen = formatted.length;
        var newStart = Math.max(0, start + (newLen - oldLen));
        if (newStart > newLen) newStart = newLen;
        el.setSelectionRange(newStart, newStart);
    });

    window.euroerpGetDateInputValue = function (id) {
        var el = document.getElementById(id);
        return el ? el.value : '';
    };
})();
