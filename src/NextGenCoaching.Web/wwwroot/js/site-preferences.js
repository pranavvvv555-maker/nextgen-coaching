(() => {
  'use strict';
  const key = 'nextgen.site.preferences.v1';
  const language = value => ['en', 'mr', 'hi'].includes(value) ? value : 'en';
  let saved;
  try { saved = JSON.parse(localStorage.getItem(key) || '{}') || {}; } catch { saved = {}; }
  let current = {
    language: language(saved.language),
    theme: ['dark', 'light'].includes(saved.theme) ? saved.theme : 'light'
  };
  function apply() {
    document.documentElement.dataset.theme = current.theme;
    document.documentElement.lang = current.language;
    document.documentElement.style.colorScheme = current.theme;
  }
  function persist() { try { localStorage.setItem(key, JSON.stringify(current)); } catch { /* Preferences still work when storage is unavailable. */ } }
  apply();
  window.nextgenPreferences = {
    read: () => ({ ...current }),
    setLanguage: value => { current.language = language(value); apply(); persist(); },
    setTheme: value => { current.theme = value === 'dark' ? 'dark' : 'light'; apply(); persist(); }
  };
})();
