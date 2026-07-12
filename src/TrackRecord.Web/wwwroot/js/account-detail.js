// Count-up de los números de la ficha de cuenta (rediseño 1b). Anima elementos .ad-countup
// de 0 al valor final con easing easeOutCubic. Respeta prefers-reduced-motion.
(function () {
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    function parse(text) {
        var m = text.match(/-?[\d.,]*\d/);
        if (!m) return null;
        var raw = m[0], normalized;
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
        if (el.dataset.countup === 'done') return;
        var original = el.textContent;
        var p = parse(original);
        if (!p) { el.dataset.countup = 'done'; return; }
        el.dataset.countup = 'done';
        var prefix = original.slice(0, p.index);
        var suffix = original.slice(p.index + p.raw.length);
        var start = null, duration = 1050;
        function frame(ts) {
            if (start === null) start = ts;
            var t = Math.min(1, (ts - start) / duration);
            var eased = 1 - Math.pow(1 - t, 3);
            el.textContent = prefix + (p.value * eased).toFixed(p.decimals) + suffix;
            if (t < 1) requestAnimationFrame(frame);
            else el.textContent = original;
        }
        requestAnimationFrame(frame);
    }

    function scan(root) {
        if (root.nodeType !== 1) return;
        if (root.classList && root.classList.contains('ad-countup')) animate(root);
        if (root.querySelectorAll) root.querySelectorAll('.ad-countup').forEach(animate);
    }

    scan(document.body);
    new MutationObserver(function (muts) {
        muts.forEach(function (m) { m.addedNodes.forEach(scan); });
    }).observe(document.body, { childList: true, subtree: true });
})();
