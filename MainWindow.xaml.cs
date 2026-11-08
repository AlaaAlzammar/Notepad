using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notepad
{
   
    public partial class MainWindow : Window
    {
          
            private bool _isDirty = false;

            private string? _currentFilePath = null;

      
            private bool _suppressDirtyTracking = false;

            private static readonly OpenFileDialog OpenDialog = new()
            {
                Title = "Select a Text File",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            private static readonly SaveFileDialog SaveDialog = new()
            {
                Title = "Save Text File",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt"
            };

            public MainWindow()
            {
                InitializeComponent();
            MainTextBox.Text = "";

                FontFamilyComboBox.ItemsSource = new InstalledFontCollection()
                    .Families
                    .Select(f => f.Name)
                    .OrderBy(name => name)
                    .ToList();
              FontFamilyComboBox.SelectedItem = MainTextBox.FontFamily.ToString();
               
                if (FontFamilyComboBox.Items.Contains("Segoe UI"))
                    FontFamilyComboBox.SelectedItem = "Segoe UI";
                else if (FontFamilyComboBox.Items.Count > 0)
                    FontFamilyComboBox.SelectedIndex = 0;

                UpdateTitleAndHeader();
                UpdateStatusBar();
            }

      

            private void NewButton_Click(object sender, RoutedEventArgs e)
            {
                if (!ConfirmDiscardChanges("creating a new file"))
                    return;

                _suppressDirtyTracking = true;
                MainTextBox.Clear();
                _suppressDirtyTracking = false;

                _currentFilePath = null;
                _isDirty = false;
                UpdateTitleAndHeader();
                UpdateStatusBar();
            }

            private void OpenButton_Click(object sender, RoutedEventArgs e)
            {
                if (!ConfirmDiscardChanges("opening another file"))
                    return;

                if (OpenDialog.ShowDialog() == true)
                {
                    try
                    {
                        string content = File.ReadAllText(OpenDialog.FileName);

                        _suppressDirtyTracking = true;
                        MainTextBox.Text = content;
                        _suppressDirtyTracking = false;

                        _currentFilePath = OpenDialog.FileName;
                        _isDirty = false;
                        UpdateTitleAndHeader();
                        UpdateStatusBar();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open the file:\n{ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            private void SaveButton_Click(object sender, RoutedEventArgs e)
            {
                SaveDocument();
            }

            private void SaveAsButton_Click(object sender, RoutedEventArgs e)
            {
                SaveDocumentAs();
            }

            private void ClearButton_Click(object sender, RoutedEventArgs e)
            {
                if (MainTextBox.Text.Length == 0)
                    return;

                var result = MessageBox.Show(
                    "Are you sure you want to clear all text? This action cannot be undone.",
                    "Confirm Clear",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _suppressDirtyTracking = false; 
                    MainTextBox.Clear();
                }
            }

            private void DateTimeButton_Click(object sender, RoutedEventArgs e)
            {
                string currentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                int caret = MainTextBox.CaretIndex;
                MainTextBox.SelectedText = string.Empty; // remove any selection
                MainTextBox.Text = MainTextBox.Text.Insert(caret, currentDateTime);
                MainTextBox.CaretIndex = caret + currentDateTime.Length;
                MainTextBox.Focus();
            }

           

            private void FontFamilyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
            {
                if (FontFamilyComboBox.SelectedItem is string fontName)
                {
                    MainTextBox.FontFamily = new FontFamily(fontName);
                }
            }

            private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {  if(MainTextBox != null)
                MainTextBox.FontSize = e.NewValue;
                if (FontSizeValueText != null)
                    FontSizeValueText.Text = ((int)e.NewValue).ToString();
            }

                private void WrapTextCheckBox_Changed(object sender, RoutedEventArgs e)
                {
                    if (MainTextBox != null)
                    { 
                        MainTextBox.TextWrapping = WrapTextCheckBox.IsChecked == true
                            ? TextWrapping.Wrap
                            : TextWrapping.NoWrap;

                        MainTextBox.HorizontalScrollBarVisibility = WrapTextCheckBox.IsChecked == true
                            ? System.Windows.Controls.ScrollBarVisibility.Disabled
                            : System.Windows.Controls.ScrollBarVisibility.Auto;
                    }
                }

            private void ReadOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
            {
                MainTextBox.IsReadOnly = ReadOnlyCheckBox.IsChecked == true;
            }

        

            private void MainTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            {
                if (!_suppressDirtyTracking)
                {
                    _isDirty = true;
                    UpdateTitleAndHeader();
                }

                UpdateStatusBar();
            }

            private void MainTextBox_SelectionChanged(object sender, RoutedEventArgs e)
            {
                UpdateStatusBar();
            }

            private bool SaveDocument()
            {
                if (_currentFilePath == null)
                {
                    return SaveDocumentAs();
                }

                return WriteToFile(_currentFilePath);
            }

            private bool SaveDocumentAs()
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    SaveDialog.FileName = System.IO.Path.GetFileName(_currentFilePath);
                    SaveDialog.InitialDirectory = System.IO.Path.GetDirectoryName(_currentFilePath);
                }

                if (SaveDialog.ShowDialog() == true)
                {
                    return WriteToFile(SaveDialog.FileName);
                }

                return false;
            }

            private bool WriteToFile(string path)
            {
                try
                {
                    File.WriteAllText(path, MainTextBox.Text, Encoding.UTF8);

                    _currentFilePath = path;
                    _isDirty = false;
                    UpdateTitleAndHeader();

                    MessageBox.Show("File saved successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not save the file:\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

 
            private bool ConfirmDiscardChanges(string actionDescription)
            {
                if (!_isDirty)
                    return true;

                var result = MessageBox.Show(
                    $"You have unsaved changes.\nDo you want to save before {actionDescription}?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                       
                        return SaveDocument();

                    case MessageBoxResult.No:
                        return true; 

                    default:
                        return false; 
                }
            }

            private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (!ConfirmDiscardChanges("closing the program"))
                {
                    e.Cancel = true;
                }
            }

          

            private void UpdateTitleAndHeader()
            {
                string fileName = _currentFilePath == null
                    ? "Untitled"
                    : System.IO.Path.GetFileName(_currentFilePath);

                string dirtyMarker = _isDirty ? "*" : string.Empty;

                CurrentFileText.Text = $"Current File: {fileName}{dirtyMarker}";
                Title = $"Mini Notepad - {fileName}{dirtyMarker}";
            }

            private void UpdateStatusBar()
            {
                string text = MainTextBox.Text;
                int caretIndex = MainTextBox.CaretIndex;

         
                int chars = text.Length;

             
                string trimmed = text.Trim();
                int words = string.IsNullOrEmpty(trimmed) ? 0 : Regex.Matches(trimmed, @"\S+").Count;


                int lineCount = Math.Max(1, MainTextBox.LineCount);

                string textBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length));
                int line = textBeforeCaret.Count(c => c == '\n') + 1;

                int lastNewLineIndex = textBeforeCaret.LastIndexOf('\n');
                int column = lastNewLineIndex == -1
                    ? caretIndex + 1
                    : caretIndex - lastNewLineIndex;

                LeftStatusText.Text = $"{chars} characters | {words} words | {lineCount} line(s)";
                RightStatusText.Text = $"Ln {line}, Col {column}";
            }
        }
    }

