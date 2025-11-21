using System;
using System.Windows;

namespace FireNet.UI.Theme
{
    public static class ThemeManager
    {
        public static string CurrentTheme { get; private set; } = "Light";

        public static void Apply(string theme)
        {
            CurrentTheme = theme;

            Application.Current.Resources.MergedDictionaries.Clear();

            var dict = new ResourceDictionary
            {
                Source = new Uri($"/Themes/{theme}.xaml", UriKind.Relative)
            };

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
