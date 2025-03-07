﻿using FirstFloor.ModernUI.Presentation;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

namespace ShowcaseApp.WPF.Content
{
    /// <summary>
    /// A simple view model for configuring theme, font and accent colors.
    /// </summary>
    public class SettingsAppearanceViewModel
        : NotifyPropertyChanged
    {
        private const string FontSmall = "small";
        private const string FontLarge = "large";

        private const string PaletteMetro = "metro";
        private const string PaletteWP = "windows phone";

        // 9 accent colors from metro design principles
        private readonly Color[] metroAccentColors =
        [
            Color.FromRgb(0x33, 0x99, 0xff),   // blue
            Color.FromRgb(0x00, 0xab, 0xa9),   // teal
            Color.FromRgb(0x33, 0x99, 0x33),   // green
            Color.FromRgb(0x8c, 0xbf, 0x26),   // lime
            Color.FromRgb(0xf0, 0x96, 0x09),   // orange
            Color.FromRgb(0xff, 0x45, 0x00),   // orange red
            Color.FromRgb(0xe5, 0x14, 0x00),   // red
            Color.FromRgb(0xff, 0x00, 0x97),   // magenta
            Color.FromRgb(0xa2, 0x00, 0xff) // purple            
        ];

        // 20 accent colors from Windows Phone 8
        private readonly Color[] wpAccentColors =
        [
            Color.FromRgb(0xa4, 0xc4, 0x00),   // lime
            Color.FromRgb(0x60, 0xa9, 0x17),   // green
            Color.FromRgb(0x00, 0x8a, 0x00),   // emerald
            Color.FromRgb(0x00, 0xab, 0xa9),   // teal
            Color.FromRgb(0x1b, 0xa1, 0xe2),   // cyan
            Color.FromRgb(0x00, 0x50, 0xef),   // cobalt
            Color.FromRgb(0x6a, 0x00, 0xff),   // indigo
            Color.FromRgb(0xaa, 0x00, 0xff),   // violet
            Color.FromRgb(0xf4, 0x72, 0xd0),   // pink
            Color.FromRgb(0xd8, 0x00, 0x73),   // magenta
            Color.FromRgb(0xa2, 0x00, 0x25),   // crimson
            Color.FromRgb(0xe5, 0x14, 0x00),   // red
            Color.FromRgb(0xfa, 0x68, 0x00),   // orange
            Color.FromRgb(0xf0, 0xa3, 0x0a),   // amber
            Color.FromRgb(0xe3, 0xc8, 0x00),   // yellow
            Color.FromRgb(0x82, 0x5a, 0x2c),   // brown
            Color.FromRgb(0x6d, 0x87, 0x64),   // olive
            Color.FromRgb(0x64, 0x76, 0x87),   // steel
            Color.FromRgb(0x76, 0x60, 0x8a),   // mauve
            Color.FromRgb(0x87, 0x79, 0x4e) // taupe
        ];

        private string selectedPalette = PaletteWP;

        private Color selectedAccentColor;
        private readonly LinkCollection themes = [];
        private Link selectedTheme;
        private string selectedFontSize;

        public SettingsAppearanceViewModel()
        {
            // add the default themes
            themes.Add(new Link { DisplayName = "dark", Source = AppearanceManager.DarkThemeSource });
            themes.Add(new Link { DisplayName = "light", Source = AppearanceManager.LightThemeSource });

            // add additional themes
            themes.Add(new Link { DisplayName = "snowflakes", Source = new Uri("/ShowcaseApp.WPF;component/Assets/ModernUI.Snowflakes.xaml", UriKind.Relative) });

            SelectedFontSize = AppearanceManager.Current.FontSize == FontSize.Large ? FontLarge : FontSmall;
            SyncThemeAndColor();

            AppearanceManager.Current.PropertyChanged += OnAppearanceManagerPropertyChanged;
        }

        private void SyncThemeAndColor()
        {
            // synchronizes the selected viewmodel theme with the actual theme used by the appearance manager.
            SelectedTheme = themes.FirstOrDefault(l => l.Source.Equals(AppearanceManager.Current.ThemeSource));

            // and make sure accent color is up-to-date
            SelectedAccentColor = AppearanceManager.Current.AccentColor;
        }

        private void OnAppearanceManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ThemeSource" || e.PropertyName == "AccentColor") {
                SyncThemeAndColor();
            }
        }

        public LinkCollection Themes => themes;

        public string[] FontSizes => [FontSmall, FontLarge];

        public string[] Palettes => [PaletteMetro, PaletteWP];

        public Color[] AccentColors => selectedPalette == PaletteMetro ? metroAccentColors : wpAccentColors;

        public string SelectedPalette
        {
            get => selectedPalette;
            set
            {
                if (selectedPalette != value) {
                    selectedPalette = value;
                    OnPropertyChanged("AccentColors");

                    SelectedAccentColor = AccentColors.FirstOrDefault();
                }
            }
        }

        public Link SelectedTheme
        {
            get => selectedTheme;
            set
            {
                if (selectedTheme != value) {
                    selectedTheme = value;
                    OnPropertyChanged("SelectedTheme");

                    // and update the actual theme
                    AppearanceManager.Current.ThemeSource = value.Source;
                }
            }
        }

        public string SelectedFontSize
        {
            get => selectedFontSize;
            set
            {
                if (selectedFontSize != value) {
                    selectedFontSize = value;
                    OnPropertyChanged("SelectedFontSize");

                    AppearanceManager.Current.FontSize = value == FontLarge ? FontSize.Large : FontSize.Small;
                }
            }
        }

        public Color SelectedAccentColor
        {
            get => selectedAccentColor;
            set
            {
                if (selectedAccentColor != value) {
                    selectedAccentColor = value;
                    OnPropertyChanged("SelectedAccentColor");

                    AppearanceManager.Current.AccentColor = value;
                }
            }
        }
    }
}
