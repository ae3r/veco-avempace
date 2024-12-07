namespace Infrastructure.Constants.Localization;

/// <summary>
/// LocalizationConstants class
/// </summary>
public static class LocalizationConstants
{
    public const string ResourcesPath = "Resources";
    public static readonly LanguageCode[] SupportedLanguages = {
            //new LanguageCode
            //{
            //    Code = "en-US",
            //    DisplayName= "English"
            //},

            new LanguageCode
            {
                Code = "fr-FR",
                DisplayName = "Français"
            }
        };
}
