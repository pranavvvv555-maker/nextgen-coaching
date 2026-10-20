// Opt-in local diagnostics (?perf). App.razor never loads this in production.
(() => {
  'use strict';
  const report = document.createElement('output');
  report.id = 'nextgen-performance';
  report.hidden = true;
  report.setAttribute('aria-hidden', 'true');
  document.body.appendChild(report);
  const frames = [], tasks = [], shifts = [];
  const visibility = [];
  const visibilityObserver = new IntersectionObserver(entries => {
    entries.forEach(entry => visibility.push({
      intersecting:entry.isIntersecting, ratio:entry.intersectionRatio,
      top:entry.boundingClientRect.top, bottom:entry.boundingClientRect.bottom,
      root:entry.rootBounds?.toJSON()
    }));
  }, {rootMargin:'-90px 0px 0px 0px'});
  let last = 0, started = 0, frame;
  const observers = [];
  for (const [type, entries] of [['longtask', tasks], ['layout-shift', shifts]]) {
    if (!PerformanceObserver.supportedEntryTypes.includes(type)) continue;
    const observer = new PerformanceObserver(list => entries.push(...list.getEntries()));
    observer.observe({type, buffered: true});
    observers.push(observer);
  }
  function snapshot() {
    const sorted = [...frames].sort((a, b) => a - b), duration = performance.now() - started;
    report.textContent = JSON.stringify({
      width: innerWidth, dpr: devicePixelRatio, seconds: +(duration / 1000).toFixed(2), frames: frames.length, visibility,
      averageFrameMs: frames.length ? +(frames.reduce((a,b) => a+b, 0) / frames.length).toFixed(2) : 0,
      p95FrameMs: +(sorted[Math.floor(sorted.length * .95)] || 0).toFixed(2),
      framesOver50ms: frames.filter(value => value > 50).length,
      longTasks: tasks.filter(task => task.startTime >= started).length,
      layoutShift: +shifts.filter(entry => entry.startTime >= started && !entry.hadRecentInput).reduce((sum, entry) => sum + entry.value, 0).toFixed(4),
      resources: performance.getEntriesByType('resource').filter(entry => entry.name.startsWith(location.origin)).map(entry => ({
        path: new URL(entry.name).pathname, transferred: entry.transferSize, decoded: entry.decodedBodySize, ms: +entry.duration.toFixed(1)
      }))
    });
  }
  function tick(now) {
    if (document.hidden) { last = 0; frame = requestAnimationFrame(tick); return; }
    if (!document.querySelector('main h1')) { frame = requestAnimationFrame(tick); return; }
    if (!started) {
      started = now;
      const scene = document.querySelector('[data-masterclass-art],[data-data-ai-scene],[data-career-journey]');
      if (scene) visibilityObserver.observe(scene);
    }
    if (last) frames.push(now - last);
    last = now;
    if (frames.length % 60 === 0) snapshot();
    if (now - started < 15000) frame = requestAnimationFrame(tick);
    else { snapshot(); report.dataset.complete = 'true'; observers.forEach(observer => observer.disconnect()); visibilityObserver.disconnect(); }
  }
  frame = requestAnimationFrame(tick);
  window.addEventListener('pagehide', () => { cancelAnimationFrame(frame); observers.forEach(observer => observer.disconnect()); visibilityObserver.disconnect(); }, {once: true});
})();
