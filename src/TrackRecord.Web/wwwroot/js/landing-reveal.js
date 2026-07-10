(function () {
    var sections = document.querySelectorAll('.lp-reveal');
    if (!sections.length) return;

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        sections.forEach(function (el) { el.classList.add('lp-in'); });
        return;
    }

    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('lp-in');
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.15 });

    sections.forEach(function (el) { observer.observe(el); });
})();
