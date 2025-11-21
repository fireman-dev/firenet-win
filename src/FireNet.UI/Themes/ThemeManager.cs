namespace FireNet.UI.Theme
{
    public static class ThemeManager
    {
        public static string CurrentTheme { get; private set; } = "Light";

        public static void ApplyTheme(string theme)
        {
            CurrentTheme = theme;

            App.Current.Resources.MergedDictionaries.Clear();

            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative)
            };

            App.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
