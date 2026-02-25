using System;
using System.Web.Mvc;
using GuardianOnline.App_Start;

namespace GuardianOnline.Controllers
{
    /// <summary>
    /// Base controller that applies localization to all derived controllers
    /// </summary>
    public class BaseController : Controller
    {
        /// <summary>
        /// Called before the action method is invoked
        /// </summary>
        /// <param name="filterContext">Information about the current request and action</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            try
            {
                // Apply localization culture based on query string or cookie
                if (filterContext != null && 
                    filterContext.HttpContext != null && 
                    filterContext.HttpContext.Request != null && 
                    filterContext.HttpContext.Response != null)
                {
                    // Pass HttpRequestBase and HttpResponseBase directly
                    LocalizationConfig.ApplyCulture(
                        filterContext.HttpContext.Request,
                        filterContext.HttpContext.Response
                    );

                    // Store language info in ViewBag for use in views
                    ViewBag.CurrentLanguage = LocalizationConfig.GetCurrentLanguage();
                    ViewBag.CurrentCultureCode = LocalizationConfig.GetCurrentCultureCode();
                    ViewBag.IsRTL = LocalizationConfig.IsRightToLeft();
                    ViewBag.LanguageDisplayName = LocalizationConfig.GetCurrentLanguageDisplayName();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break the request
                System.Diagnostics.Debug.WriteLine(string.Format("BaseController Localization Error: {0}", ex.Message));
            }

            base.OnActionExecuting(filterContext);
        }

        /// <summary>
        /// Gets the current language code (ar or en)
        /// </summary>
        protected string CurrentLanguage
        {
            get
            {
                try
                {
                    return LocalizationConfig.GetCurrentLanguage();
                }
                catch
                {
                    return "en";
                }
            }
        }

        /// <summary>
        /// Gets the current culture code (ar-EG or en-US)
        /// </summary>
        protected string CurrentCultureCode
        {
            get
            {
                try
                {
                    return LocalizationConfig.GetCurrentCultureCode();
                }
                catch
                {
                    return "en-US";
                }
            }
        }

        /// <summary>
        /// Checks if the current language is Right-to-Left
        /// </summary>
        protected bool IsRightToLeft
        {
            get
            {
                try
                {
                    return LocalizationConfig.IsRightToLeft();
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the display name for the current language
        /// </summary>
        protected string CurrentLanguageDisplayName
        {
            get
            {
                try
                {
                    return LocalizationConfig.GetCurrentLanguageDisplayName();
                }
                catch
                {
                    return "English";
                }
            }
        }
    }
}