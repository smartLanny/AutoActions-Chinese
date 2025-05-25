using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoActions.Views
{
    /// <summary>
    /// Interaktionslogik für UserAppSettings.xaml
    /// </summary>
    public partial class UserAppSettingsView : UserControl
    {
        public UserAppSettingsView()
        {
            InitializeComponent();
            Loaded += UserAppSettingsView_Loaded;
        }

        private void UserAppSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserAppSettings settings)
            {
                // 设置初始选中的语言项
                var comboBox = GetLanguageComboBox();
                if (comboBox != null)
                {
                    foreach (ComboBoxItem item in comboBox.Items)
                    {
                        if (item.Tag.ToString() == settings.SelectedLanguage)
                        {
                            comboBox.SelectedValue = item.Tag.ToString();
                            break;
                        }
                    }
                }
            }
        }

        private ComboBox GetLanguageComboBox()
        {
            // 查找语言下拉框控件
            ComboBox languageComboBox = null;
            
            // 使用名称查找
            var grid = this.Content as Grid;
            if (grid != null)
            {
                var scrollViewer = grid.Children[2] as ScrollViewer;
                if (scrollViewer != null)
                {
                    var innerGrid = scrollViewer.Content as Grid;
                    if (innerGrid != null)
                    {
                        foreach (var child in innerGrid.Children)
                        {
                            if (child is ComboBox comboBox)
                            {
                                // 查找绑定到SelectedLanguage的ComboBox
                                var binding = comboBox.GetBindingExpression(ComboBox.SelectedValueProperty);
                                if (binding != null && binding.ParentBinding.Path.Path == "SelectedLanguage")
                                {
                                    languageComboBox = comboBox;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            return languageComboBox;
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
