using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace GuardianOnline.App_Start
{
    /// <summary>
    /// Localization configuration for Arabic/English language switching
    /// </summary>
    public static class LocalizationConfig
    {
        private const string COOKIE_NAME = "lang";
        private const string DEFAULT_LANGUAGE = "en";
        
        // Supported languages with their culture codes
        private static readonly string[] SupportedLanguages = { "ar", "en" };
        
        private static readonly System.Collections.Generic.Dictionary<string, string> LanguageToCulture = 
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "ar", "ar-EG" },
                { "en", "en-US" }
            };

        /// <summary>
        /// Gets the culture code for the specified language
        /// </summary>
        private static string GetCultureCode(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return LanguageToCulture[DEFAULT_LANGUAGE];
            }

            language = language.Trim().ToLowerInvariant();

            if (LanguageToCulture.ContainsKey(language))
            {
                return LanguageToCulture[language];
            }

            return LanguageToCulture[DEFAULT_LANGUAGE];
        }

        /// <summary>
        /// Validates if the language code is supported
        /// </summary>
        private static bool IsValidLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return false;
            }

            language = language.Trim().ToLowerInvariant();
            
            return SupportedLanguages.Contains(language);
        }

        /// <summary>
        /// Applies culture settings based on query string or cookie (using base classes)
        /// </summary>
        /// <param name="request">HTTP request base</param>
        /// <param name="response">HTTP response base</param>
        public static void ApplyCulture(HttpRequestBase request, HttpResponseBase response)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            string selectedLanguage = DEFAULT_LANGUAGE;
            bool shouldUpdateCookie = false;

            try
            {
                // Priority 1: Check query string parameter
                string queryLang = request.QueryString["lang"];
                if (!string.IsNullOrWhiteSpace(queryLang))
                {
                    queryLang = queryLang.Trim().ToLowerInvariant();
                    
                    if (IsValidLanguage(queryLang))
                    {
                        selectedLanguage = queryLang;
                        shouldUpdateCookie = true;
                    }
                }
                else
                {
                    // Priority 2: Check cookie
                    HttpCookie langCookie = request.Cookies[COOKIE_NAME];
                    if (langCookie != null && !string.IsNullOrWhiteSpace(langCookie.Value))
                    {
                        string cookieLang = langCookie.Value.Trim().ToLowerInvariant();
                        
                        if (IsValidLanguage(cookieLang))
                        {
                            selectedLanguage = cookieLang;
                        }
                        else
                        {
                            // Cookie has invalid value, update it
                            shouldUpdateCookie = true;
                        }
                    }
                    else
                    {
                        // No cookie exists, create one
                        shouldUpdateCookie = true;
                    }
                }

                // Set the culture for the current thread
                string cultureCode = GetCultureCode(selectedLanguage);
                CultureInfo culture = new CultureInfo(cultureCode);
                
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // Update cookie if needed
                if (shouldUpdateCookie)
                {
                    SetLanguageCookie(response, selectedLanguage);
                }
            }
            catch (CultureNotFoundException ex)
            {
                // Log culture error and fall back to default
                System.Diagnostics.Debug.WriteLine(string.Format("Culture not found: {0}", ex.Message));
                ApplyDefaultCulture(response);
            }
            catch (Exception ex)
            {
                // Log error silently and fall back to default
                System.Diagnostics.Debug.WriteLine(string.Format("Localization Error: {0}", ex.Message));
                ApplyDefaultCulture(response);
            }
        }

        /// <summary>
        /// Applies the default culture
        /// </summary>
        private static void ApplyDefaultCulture(HttpResponseBase response)
        {
            try
            {
                CultureInfo defaultCulture = new CultureInfo(GetCultureCode(DEFAULT_LANGUAGE));
                Thread.CurrentThread.CurrentCulture = defaultCulture;
                Thread.CurrentThread.CurrentUICulture = defaultCulture;
                
                if (response != null)
                {
                    SetLanguageCookie(response, DEFAULT_LANGUAGE);
                }
            }
            catch
            {
                // If even default fails, ignore silently
            }
        }

        /// <summary>
        /// Sets the language cookie
        /// </summary>
        private static void SetLanguageCookie(HttpResponseBase response, string language)
        {
            try
            {
                HttpCookie langCookie = new HttpCookie(COOKIE_NAME)
                {
                    Value = language,
                    Expires = DateTime.Now.AddYears(1),
                    HttpOnly = false,
                    Path = "/"
                };

                // Clear existing cookie first to avoid duplicates
                response.Cookies.Set(langCookie);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error setting language cookie: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Gets the current language from the thread culture
        /// </summary>
        public static string GetCurrentLanguage()
        {
            try
            {
                string cultureName = Thread.CurrentThread.CurrentCulture.Name;
                
                if (cultureName.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
                {
                    return "ar";
                }
                
                if (cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    return "en";
                }
                
                return DEFAULT_LANGUAGE;
            }
            catch
            {
                return DEFAULT_LANGUAGE;
            }
        }

        /// <summary>
        /// Gets the current culture code
        /// </summary>
        public static string GetCurrentCultureCode()
        {
            try
            {
                return Thread.CurrentThread.CurrentCulture.Name;
            }
            catch
            {
                return GetCultureCode(DEFAULT_LANGUAGE);
            }
        }

        /// <summary>
        /// Checks if the current culture is RTL (Right-to-Left)
        /// </summary>
        public static bool IsRightToLeft()
        {
            try
            {
                return Thread.CurrentThread.CurrentCulture.TextInfo.IsRightToLeft;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the display name for the current language
        /// </summary>
        public static string GetCurrentLanguageDisplayName()
        {
            string lang = GetCurrentLanguage();
            return lang == "ar" ? "العربية" : "English";
        }

        /// <summary>
        /// Gets all supported languages
        /// </summary>
        public static string[] GetSupportedLanguages()
        {
            return (string[])SupportedLanguages.Clone();
        }
    }
}