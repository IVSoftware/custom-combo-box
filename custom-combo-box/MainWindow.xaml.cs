using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace custom_combo_box
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            comboBox.SelectionChanged += (sender, e) => Title = $"Main Window {comboBox.SelectedItem}";

            #region I N I T I A L I Z E    T E S T    V A L U E S
            // DataContext.ParameterValueList = new[] { 1, 2, 3 };   // Just testing...
            DataContext.ParameterValueList = new[] { "Dogs", "Cats", "Pets"};
            #endregion I N I T I A L I Z E    T E S T    V A L U E S
        }
        new MainWindowDataContext DataContext => (MainWindowDataContext)base.DataContext;
    }
    class MainWindowDataContext : INotifyPropertyChanged
    {
        public IList ParameterValueList
        {
            get => _parameterValueList;
            set
            {
                if (!Equals(_parameterValueList, value))
                {
                    _parameterValueList = value;
                    OnPropertyChanged();
                }
            }
        }
        IList? _parameterValueList = default;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    class CustomComboBox : ComboBox
    {
        public CustomComboBox()
        {
            Height = 30;
            Width = 150;
            BorderBrush = new SolidColorBrush(Colors.Teal);
            BorderThickness = new Thickness(2);
            IsEditable = true;
            IsReadOnly = true;
            Focusable = false;
            IsTextSearchEnabled = false;
            ItemTemplate = CreateCheckboxTemplate();
            CheckedItems.CollectionChanged += (sender, e) => Refresh(sender, null);
        }
        void Refresh(object? sender, RoutedEventArgs? e)
        {
            var countDisplay = CheckedItems.Count == 0
                ? string.Empty
                : $"[{CheckedItems.Count}] ";

            if (CheckedItems.Count == 0)
            {
                Text = SelectedItem?.ToString() ?? string.Empty;
            }
            else
            {
                var selectedNotChecked = string.Empty;
                if (SelectedItem is not null && !CheckedItems.Contains(SelectedItem))
                {
                    selectedNotChecked = $" : [{SelectedItem}]";
                }
                Text = $"{countDisplay} {string.Join(";", CheckedItems.Select(_ => localFormatItem(_)))}{selectedNotChecked}";

                string localFormatItem(object item)
                {
                    if (SelectedItem == item)
                    {
                        return $"[{item?.ToString() ?? " "}]";
                    }
                    else
                    {
                        return item?.ToString() ?? string.Empty;
                    }
                }
            }
        }
        private DataTemplate CreateCheckboxTemplate()
        {
            var dataTemplate = new DataTemplate(typeof(string));
            var checkBoxFactory = new FrameworkElementFactory(typeof(CheckBox));
            checkBoxFactory.SetBinding(ContentControl.ContentProperty, new Binding("."));
            checkBoxFactory.SetValue(CheckBox.FontSizeProperty, 14.0);
            checkBoxFactory.SetValue(CheckBox.ForegroundProperty, Brushes.DarkSlateBlue); 
            checkBoxFactory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(OnCheckBoxChecked));
            checkBoxFactory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(OnCheckBoxUnchecked));
            dataTemplate.VisualTree = checkBoxFactory;
            return dataTemplate;
        }
        private void OnCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox) CheckedItems.Add(checkbox.DataContext);
        }
        private void OnCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox) CheckedItems.Remove(checkbox.DataContext);
        }
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            Refresh(this, new RoutedEventArgs());
        }        

        public ObservableCollection<object> CheckedItems { get; } = new ObservableCollection<object>();
    }
}