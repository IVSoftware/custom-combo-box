As I understand it, you want your custom combo box to display something different that the string representation of the selected item, and in particular to show some indication of the items that are checked. As one approach, something like this might be easier if you just make an actual `CustomComboBox` subclass and do all of this internally. But regardless of whether you make a custom control, one way to achieve your objective is to set the `ComboBox` properties to `IsEditable`, `IsReadOnly`, !`Focusable` and (very importantly) !`IsTextSearchEnabled`.

___

_Depending on your implementation, the `IsTextSearchEnabled` value could explain why you're getting a blank, because by default the `ComboBox` will attempt to match the displayed text with an existing item. If the display is something like for examle **"[2]  Dogs;Cats"** then the selection will get reset to -1 and the text will disappear._

___

Here's the kind of thing I mean, where the check box data template is internal to the control and being checked and being selected are separate. In the list of checked items, an item that is _also_ selected (by clicking the text portion of the combo) then it appears in  [brackets]. This might not be "exactly" what you intend, but hopefully will serve as a starting point for what you're doing. 

___

**C#**


[![screenshot][1]][1]

~~~csharp
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
        SemaphoreSlim IsRefreshing = new SemaphoreSlim(1,1);
        void Refresh(object? sender, RoutedEventArgs? e)
        {
            if (IsRefreshing.Wait(0))
            {
                try
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
                finally
                {
                    IsRefreshing.Release();
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
~~~

___
**XAML**

Since the `DataTemplate` is now baked into the control, all that needs to be done here is to bind the data source.

~~~xaml
<Window x:Class="custom_combo_box.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:custom_combo_box"
        mc:Ignorable="d"
        Title="MainWindow" Height="300" Width="500">
    <Window.DataContext>
        <local:MainWindowDataContext>
        </local:MainWindowDataContext>
    </Window.DataContext>
    <Grid>
        <local:CustomComboBox x:Name="comboBox" ItemsSource="{Binding ParameterValueList}"/>
    </Grid>
</Window>
~~~

___

**Initialize with Test Values**

~~~
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
~~~

  [1]: https://i.sstatic.net/pUh6nnfg.png