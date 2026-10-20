using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;

namespace NextGenCoaching.Web.Services;

public sealed class SitePreferences(IJSRuntime js)
{
    private static readonly Dictionary<string, string[]> Translations = LoadTranslations();
    private readonly Dictionary<string, string> _translationCache = new(StringComparer.Ordinal);
    private Task? _initialization;
    public string Language { get; private set; } = "en";
    public string Theme { get; private set; } = "light";
    public int Revision { get; private set; }
    public event Action? Changed;
    public event Action? ThemeChanged;
    public string this[string? source] => Translate(source);

    public string Translate(string? source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;
        if (Language == "en") return WebUtility.HtmlDecode(source);
        if (_translationCache.TryGetValue(source, out var cached)) return cached;
        var translated = TranslateUncached(source);
        // Bound circuit memory when user-supplied labels or server errors vary over time.
        if (_translationCache.Count < 2048) _translationCache[source] = translated;
        return translated;
    }

    private string TranslateUncached(string source)
    {
        var key = Regex.Replace(WebUtility.HtmlDecode(source), @"\s+", " ").Trim();
        if (Language != "en" && Translations.TryGetValue(key, out var values))
            return values[Language == "mr" ? 0 : 1];
        if (Language != "en" && key.StartsWith("Request failed with status ", StringComparison.Ordinal) && int.TryParse(key[27..], out var status))
            return Format("Request failed with status {0}", status);
        return WebUtility.HtmlDecode(source);
    }

    public CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "en" ? "en-IN" : $"{Language}-IN");

    public string Format(string source, params object[] values) =>
        string.Format(Culture, this[source], values);

    public Task InitializeAsync() => _initialization ??= RestoreAsync();
    private async Task RestoreAsync()
    {
        var saved = await js.InvokeAsync<PreferenceSnapshot>("nextgenPreferences.read");
        var language = ValidLanguage(saved.Language);
        var theme = saved.Theme == "dark" ? "dark" : "light";
        var languageChanged = Language != language;
        var themeChanged = Theme != theme;
        Language = language;
        Theme = theme;
        if (languageChanged) { _translationCache.Clear(); Notify(); }
        else if (themeChanged) ThemeChanged?.Invoke();
    }

    public async Task SetLanguageAsync(string? language)
    {
        var nextLanguage = ValidLanguage(language);
        if (Language == nextLanguage) return;
        Language = nextLanguage;
        _translationCache.Clear();
        await js.InvokeVoidAsync("nextgenPreferences.setLanguage", Language);
        Notify();
    }

    public async Task ToggleThemeAsync()
    {
        Theme = Theme == "dark" ? "light" : "dark";
        await js.InvokeVoidAsync("nextgenPreferences.setTheme", Theme);
        ThemeChanged?.Invoke();
    }

    private void Notify() { Revision++; Changed?.Invoke(); }
    private static string ValidLanguage(string? value) => value is "mr" or "hi" ? value : "en";
    private sealed record PreferenceSnapshot(string Language, string Theme);
    private static Dictionary<string, string[]> LoadTranslations()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NextGenCoaching.Web.Resources.Translations.json")
            ?? throw new InvalidOperationException("The site translation catalog is missing.");
        var entries = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream) ?? new();
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries) result[entry.Key] = entry.Value;
        return result;
    }
}
