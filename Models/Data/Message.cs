using SerialPortDevicesTestEnvironment.Helpers;

namespace SerialPortDevicesTestEnvironment.Models.Data
{
    public class Message : BindableBase
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
    }
}
