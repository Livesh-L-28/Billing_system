using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BillingSoftware.UI.Views;

public partial class BillingView : UserControl
{
    public BillingView()
    {
        InitializeComponent();
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            var element = e.OriginalSource as UIElement;
            if (element != null)
            {
                if (element is Button)
                {
                    // Let buttons process Enter normally (trigger Click/Command)
                    return;
                }

                if (element is TextBox textBox && textBox.AcceptsReturn)
                {
                    // Let multiline TextBoxes handle Enter normally
                    return;
                }

                var comboBox = FindVisualParent(element, typeof(ComboBox)) as ComboBox;
                if (comboBox != null)
                {
                    if (comboBox.IsDropDownOpen)
                    {
                        // If no item is selected, automatically select the first filtered item
                        if (comboBox.SelectedItem == null && comboBox.Items.Count > 0)
                        {
                            comboBox.SelectedItem = comboBox.Items[0];
                        }
                        
                        // Close the dropdown
                        comboBox.IsDropDownOpen = false;
                        e.Handled = true;
                        
                        // Move focus to the next logical control
                        element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        return;
                    }
                }

                // Move focus to the next logical control in TabIndex order
                e.Handled = true;
                element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
    }

    private void CustomerComboBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.DataContext is ViewModels.BillingViewModel vm)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Tab)
            {
                return;
            }

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            int selectionStart = textBox?.SelectionStart ?? 0;
            int selectionLength = textBox?.SelectionLength ?? 0;

            vm.CustomerSearchText = comboBox.Text;

            if (textBox != null)
            {
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;
            }

            if (!comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }
    }

    private void ProductComboBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.DataContext is ViewModels.BillingViewModel vm)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Tab)
            {
                return;
            }

            var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            int selectionStart = textBox?.SelectionStart ?? 0;
            int selectionLength = textBox?.SelectionLength ?? 0;

            vm.ProductSearchText = comboBox.Text;

            if (textBox != null)
            {
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = selectionLength;
            }

            if (!comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = true;
            }
        }
    }

    private static DependencyObject? FindVisualParent(DependencyObject child, Type parentType)
    {
        DependencyObject parentDep = child;
        while (parentDep != null)
        {
            parentDep = System.Windows.Media.VisualTreeHelper.GetParent(parentDep);
            if (parentDep != null && parentType.IsAssignableFrom(parentDep.GetType()))
            {
                return parentDep;
            }
        }
        return null;
    }
}
