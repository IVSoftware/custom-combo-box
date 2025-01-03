using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

            var checkBoxStyle = new Style(typeof(CheckBox));

            // Use a MultiBinding for the DataTrigger
            var multiBinding = new MultiBinding
            {
                Converter = new SelectedItemMatchMultiConverter()
            };

            // Bind the SelectedItem of the ComboBox
            multiBinding.Bindings.Add(new Binding
            {
                Path = new PropertyPath("SelectedItem"),
                RelativeSource = new RelativeSource
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorType = typeof(CustomComboBox)
                }
            });

            // Bind the current CheckBox's DataContext
            multiBinding.Bindings.Add(new Binding("."));

            var selectedTrigger = new DataTrigger
            {
                Binding = multiBinding
            };

            // Apply Setters
            selectedTrigger.Setters.Add(new Setter(CheckBox.BackgroundProperty, Brushes.CornflowerBlue));
            selectedTrigger.Setters.Add(new Setter(CheckBox.ForegroundProperty, Brushes.White));
            checkBoxStyle.Triggers.Add(selectedTrigger);

            checkBoxFactory.SetValue(CheckBox.StyleProperty, checkBoxStyle);
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

    internal class SelectedItemMatchMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2) return false;

            var selectedItem = values[0]; // The SelectedItem of the ComboBox
            var checkBoxDataContext = values[1]; // The DataContext of the CheckBox
            Debug.WriteLine(Equals(selectedItem, checkBoxDataContext));
            return Equals(selectedItem, checkBoxDataContext);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}