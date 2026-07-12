(function () {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    function parseNumber(text) {
        var m = text.match(/-?[\d.,]*\d/);
        if (!m) return null;
        var raw = m[0];
        var normalized;
        if (raw.indexOf(',') > -1 && raw.indexOf('.') > -1) {
            normalized = raw.lastIndexOf(',') > raw.lastIndexOf('.')
                ? raw.replace(/\./g, '').replace(',', '.')
                : raw.replace(/,/g, '');
        } else if (raw.indexOf(',') > -1) {
            var afterComma = raw.split(',').pop();
            normalized = afterComma.length <= 2 ? raw.replace(',', '.') : raw.replace(/,/g, '');
        } else {
            normalized = raw;
        }
        var value = parseFloat(normalized);
        if (isNaN(value)) return null;
        return { value: value, index: m.index, raw: raw, decimals: (normalized.split('.')[1] || '').length };
    }

    function animate(el) {
        // Los valores hero cambian al filtrar por periodo; si el count-up reemplaza su textContent
        // desengancha el nodo de texto que Blazor rastrea y el valor se congela en el de la primera
        // carga (la gráfica SVG y el meter sí se repintan, el número no). Se excluyen del count-up.
        if (el.classList.contains('hero-value')) return;
        if (el.dataset.countup === 'done') return;
        var original = el.textContent;
        var parsed = parseNumber(original);
        if (!parsed) { el.dataset.countup = 'done'; return; }
        el.dataset.countup = 'done';

        var prefix = original.slice(0, parsed.index);
        var suffix = original.slice(parsed.index + parsed.raw.length);
        var start = null;
        var duration = 950;

        function frame(ts) {
            if (start === null) start = ts;
            var t = Math.min(1, (ts - start) / duration);
            var eased = 1 - Math.pow(1 - t, 3);
            el.textContent = prefix + (parsed.value * eased).toFixed(parsed.decimals) + suffix;
            if (t < 1) requestAnimationFrame(frame);
            else el.textContent = original;
        }
        requestAnimationFrame(frame);
    }

    function scan(root) {
        if (root.nodeType !== 1) return;
        if (root.classList && root.classList.contains('stat-value')) animate(root);
        if (root.querySelectorAll) root.querySelectorAll('.stat-value').forEach(animate);
    }

    scan(document.body);

    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (m) {
            m.addedNodes.forEach(scan);
        });
    });
    observer.observe(document.body, { childList: true, subtree: true });
})();
