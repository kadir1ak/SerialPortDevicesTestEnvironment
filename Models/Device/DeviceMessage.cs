using SerialPortDevicesTestEnvironment.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortDevicesTestEnvironment.Models.Device
{
    public class DeviceMessage : BindableBase
    {
        // Gelen mesaj için bir “index” (örneğin, kaçıncı gelen mesaj olduğu)
        private int _incomingMessageIndex;
        public int IncomingMessageIndex
        {
            get => _incomingMessageIndex;
            set => SetProperty(ref _incomingMessageIndex, value);
        }

        // Gelen mesaj (Incoming)
        private string _incomingMessage;
        public string IncomingMessage
        {
            get => _incomingMessage;
            set => SetProperty(ref _incomingMessage, value);
        }

        // Giden mesaj (Outgoing)
        //private string _outgoingMessage;
        //public string OutgoingMessage
        //{
        //    get => _outgoingMessage;
        //    set => SetProperty(ref _outgoingMessage, value);
        //}        
    }
}
