using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using SerialPortDevicesTestEnvironment.ViewModels.DeviceViewModels;

namespace SerialPortDevicesTestEnvironment.Helpers
{
    public class PortConnectedToBrushMultiConverter : IMultiValueConverter
    {
        public Brush ConnectedBrush { get; set; } = Brushes.Green;
        public Brush DisconnectedBrush { get; set; } = Brushes.Gray;

        // values[0]: port adı (string)
        // values[1]: DevicesViewModel (DataContext)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string portName = values[0] as string;
            var vm = values[1] as DevicesViewModel;

            if (string.IsNullOrEmpty(portName) || vm == null)
                return DisconnectedBrush;

            // ViewModel içindeki "IsPortConnected(portName)" metodu ile bağlı mı kontrol et
            bool isConnected = vm.IsPortConnected(portName);
            return isConnected ? ConnectedBrush : DisconnectedBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
