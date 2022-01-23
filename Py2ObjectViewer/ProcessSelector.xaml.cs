using EveAutomation.memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Py2ObjectViewer
{
    /// <summary>
    /// Interaction logic for ProcessSelector.xaml
    /// </summary>
    public partial class ProcessSelector : Window, INotifyPropertyChanged
    {

        public event EventHandler<Process>? ProcessSelected;

        public ProcessSelector()
        {
            InitializeComponent();
        }

        public List<Process> Processes
        {
            get {
                var list = new List<Process>();
                foreach (Process process in Process.GetProcesses())
                {
                    var handle = WinApi.OpenProcess((int)(WinApi.ProcessAccessFlags.VirtualMemoryRead | WinApi.ProcessAccessFlags.QueryInformation), 
                        false, process.Id);
                    if (handle != IntPtr.Zero)
                    {
                        list.Add(process);
                        WinApi.CloseHandle(handle);
                    }
                }
                return list;
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RefreshClicked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(nameof(Processes));
        }

        private void ItemProcessDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListViewItem viewItem)
                return;

            if (viewItem.Content is not Process process)
                return;

            ProcessSelected?.Invoke(this, process);
            Close();
        }
    }

    public class ProcessToIconImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not Process process)
                return null;
            
            try
            {
                if (process.MainModule is null || process.MainModule.FileName is null)
                    return null;
            } catch (InvalidOperationException)
            {
                return null;
            }


            using var ico = Icon.ExtractAssociatedIcon(process.MainModule.FileName);
            if (ico == null)
                return null;

            ImageSource source = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "yes";
                else
                    return "no";
            }
            return "no";
        }
    }
}
