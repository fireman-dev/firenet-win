using System;
using System.Windows;

namespace FireNet.UI.Theme
{
    public static class ThemeManager
    {
        public static string CurrentTheme { get; private set; } = "Light";

        public static void ApplyTheme(string theme)
        {
            CurrentTheme = theme;

            // پاک کردن Resourceهای قبلی
            Application.Current.Resources.MergedDictionaries.Clear();

            // لود کردن تم جدید
            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
