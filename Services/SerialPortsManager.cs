using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SerialPortDevicesTestEnvironment.Services
{
    public class SerialPortsManager : IDisposable
    {
        // WMI üzerinden seri portların çıkarılmasını izlemek için kullanılan watcher
        private ManagementEventWatcher _serialPortsRemovedWatcher;
        // WMI üzerinden seri portların eklenmesini izlemek için kullanılan watcher
        private ManagementEventWatcher _serialPortsAddedWatcher;

        /// <summary>
        /// Sistemde mevcut olan, yani kullanılabilir durumdaki seri portların
        /// adlarını (ör: "COM3", "COM5" vb.) tutan bir liste.
        /// Bu liste, WMI event'ları ile otomatik güncellenir.
        /// </summary>
        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Uygulamada şu anda "ConnectToPort" ile açılmış ve aktif bağlantısı bulunan
        /// seri port nesnelerinin koleksiyonu.
        /// </summary>
        public ObservableCollection<SerialPort> ConnectedPorts { get; } = new ObservableCollection<SerialPort>();

        /// <summary>
        /// Her porta ait bir veri kuyruğu tutar. Portun adı (ör: "COM3") → gelen ham verileri
        /// sakladığımız "BlockingCollection<string>".
        /// </summary>
        private readonly ConcurrentDictionary<string, BlockingCollection<string>> _portDataQueues = new();

        /// <summary>
        /// Her port için arka planda veri işleyen Task'leri tutar. Port adı → Task.
        /// </summary>
        private readonly ConcurrentDictionary<string, Task> _portProcessingTasks = new();

        /// <summary>
        /// Her port için iptal tokeni tutar. Bağlantı kesildiğinde veya uygulama kapanırken
        /// bu token iptal edilerek arka plan iş parçacığını durdurur.
        /// </summary>
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _portCancellationTokens = new();

        // Yeni bir port eklendiğinde (fiziksel olarak takıldığında) tetiklenen event
        public event Action<string> SerialPortAdded;
        // Port çıkarıldığında (fiziksel olarak) tetiklenen event
        public event Action<string> SerialPortRemoved;
        // Herhangi bir porttan yeni veri (string) geldiğinde tetiklenen event
        // parametre olarak (PortName, Message) döner.
        public event Action<string, string> MessageReceived;

        /// <summary>
        /// Kurucu metod: WMI event watcher'larını başlatır ve ilk defa port taraması yapar.
        /// </summary>
        public SerialPortsManager()
        {
            InitializeEventWatchers();
            ScanSerialPorts();
        }
        public void SendMessage(string portName, string message)
        {
            var serialPort = ConnectedPorts.FirstOrDefault(p => p.PortName == portName);
            if (serialPort != null && serialPort.IsOpen)
            {
                // Burada satır sonu eklemek için "\r\n" gerekebilir 
                // (cihazınızın protokolüne göre).
                serialPort.WriteLine(message);
            }
            else
            {
                // Port yoksa veya kapalıysa istek reddedilebilir / uyarı verilebilir
                // throw new InvalidOperationException("Port not found or not open.");
            }
        }

        #region Serial Port Management

        /// <summary>
        /// Belirtilen port adına (örneğin "COM3") belirtilen baud hızında bağlanmaya çalışır.
        /// Başarıyla bağlanırsa, "ConnectedPorts" koleksiyonuna ekler ve veri işleme sürecini başlatır.
        /// </summary>
        /// <param name="portName">COM port adı (örn: "COM3")</param>
        /// <param name="baudRate">Baud hızı (varsayılan 9600)</param>
        public void ConnectToPort(string portName, int baudRate = 9600)
        {
            try
            {
                // Eğer ilgili port zaten "ConnectedPorts" içinde varsa hata veriyoruz
                // (örn. aynı porta ikinci kez bağlanmaya çalışmak)
                if (ConnectedPorts.Any(p => p.PortName == portName))
                    throw new InvalidOperationException($"Port {portName} is already connected.");

                // Seri port nesnesi oluşturup ayarlarını yap
                var serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 5000,
                    WriteTimeout = 5000
                };

                // DataReceived event'i tetiklenince OnDataReceived metodunu çağırır
                serialPort.DataReceived += (s, e) => OnDataReceived(serialPort);
                // Portu aç
                serialPort.Open();

                // Başarıyla açıldıysa, ConnectedPorts koleksiyonuna ekle
                ConnectedPorts.Add(serialPort);
                // Bu port için arka plan veri işleme task'ini başlat
                StartProcessingPortData(serialPort);
            }
            catch (Exception ex)
            {
                // Herhangi bir hata oluşursa kullanıcıya mesaj gösteriyoruz
                MessageBox.Show($"Error connecting to port {portName}: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Belirtilen portu bağlantısını keser ve kapatır. Böylece arka plan işlemeyi de durdurur.
        /// </summary>
        public void DisconnectFromPort(string portName)
        {
            try
            {
                // Önce ConnectedPorts listesinde var mı diye bakıyoruz
                var serialPort = ConnectedPorts.FirstOrDefault(p => p.PortName == portName);
                if (serialPort == null) return; // Yoksa hiçbir şey yapma

                // Arka planda çalışan veri işleme görevini durdur (iptal et)
                StopProcessingPortData(portName);

                // Portu kapat
                serialPort.Close();
                // Koleksiyondan çıkar
                ConnectedPorts.Remove(serialPort);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting from port {portName}: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Şu anda bağlı olan portların string ad listesini döndürür ("COM3", "COM4", vb.)
        /// </summary>
        public IEnumerable<string> GetConnectedPorts()
        {
            return ConnectedPorts.Select(p => p.PortName);
        }

        #endregion

        #region Data Processing

        /// <summary>
        /// DataReceived event'i tetiklendiğinde çağrılan metod.
        /// SerialPort üzerinden ReadExisting() ile o anda gelen tüm ham veriyi alır
        /// ve ilgili portun kuyruğuna ekler.
        /// </summary>
        private void OnDataReceived(SerialPort serialPort)
        {
            try
            {
                // O anda porttan okunacak verileri string olarak al
                string data = serialPort.ReadExisting();
                // Eğer veri boş değilse
                if (!string.IsNullOrEmpty(data))
                {
                    // Portun adına göre dictionary'deki kuyruğu bul
                    if (_portDataQueues.TryGetValue(serialPort.PortName, out var queue))
                    {
                        // Kuyruğa ekle (arka plan thread'i bu kuyruğu okuyacak)
                        queue.Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading data from port {serialPort.PortName}: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Bağlanılan bir port için arka planda veri işleme task'ini başlatır.
        /// - PortName'e karşılık gelen bir BlockingCollection (kuyruk) oluşturur.
        /// - CancellationTokenSource ayarlayarak iptal kontrolü sağlar.
        /// - Task.Run ile arka planda ProcessData'yi çağıran bir döngü oluşturur.
        /// </summary>
        private void StartProcessingPortData(SerialPort serialPort)
        {
            var portName = serialPort.PortName;

            // Bu porta özel bir iptal tokeni oluşturuyoruz
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Bu porta özel bir BlockingCollection yarat
            var dataQueue = new BlockingCollection<string>();
            _portDataQueues[portName] = dataQueue;
            _portCancellationTokens[portName] = cancellationTokenSource;

            // Arka planda veri işleyen Task
            var processingTask = Task.Run(() =>
            {
                try
                {
                    // Kuyruktan veri geldikçe ProcessData metodunu çağır
                    // GetConsumingEnumerable, kuyruğa eklenmiş her bir veriyi sırasıyla okur.
                    foreach (var data in dataQueue.GetConsumingEnumerable(cancellationToken))
                    {
                        ProcessData(portName, data);
                    }
                }
                catch (OperationCanceledException)
                {
                    // İptal durumunda normal olarak yakalanan exception
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing data for port {portName}: {ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, cancellationToken);

            // Oluşturduğumuz processingTask'i dictionary'ye kaydedelim
            _portProcessingTasks[portName] = processingTask;
        }

        /// <summary>
        /// Bir portun veri işleme task'ini durdurmak (bağlantı koparıldığında veya Dispose ederken).
        /// </summary>
        private void StopProcessingPortData(string portName)
        {
            // 1) İlgili iptal tokeni bul ve iptal et
            if (_portCancellationTokens.TryRemove(portName, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            // 2) Veri kuyruğunu dictionary'den kaldır ve kapat (CompleteAdding)
            if (_portDataQueues.TryRemove(portName, out var dataQueue))
            {
                dataQueue.CompleteAdding();
            }

            // 3) Arka plandaki görevi dictionary'den al, bitmesini bekle
            if (_portProcessingTasks.TryRemove(portName, out var processingTask))
            {
                processingTask.Wait();
            }
        }

        /// <summary>
        /// Arka planda okunan her bir veri satırı burada işlenir.
        /// Örneğin, veriyi parse edebilir, sample rate hesaplayabilir, vb.
        /// Burada sadece "MessageReceived" event'i tetiklenir.
        /// </summary>
        private void ProcessData(string portName, string data)
        {
            // Bu örnekte sadece event fırlatıyoruz.
            // "MessageReceived" event'i, (portName, data) bilgisini veriyor.
            // Bu sayede UI katmanında veya başka bir yerde yakalanıp işlenebilir.
            MessageReceived?.Invoke(portName, data);
        }

        #endregion

        #region Serial Port Detection

        /// <summary>
        /// Seri port ekleme/çıkarma eventlerini yakalamak için ManagementEventWatcher'ları başlatır.
        /// </summary>
        private void InitializeEventWatchers()
        {
            try
            {
                // 1) Seri portun çıkarılmasını izler (EventType=3 => removal)
                _serialPortsRemovedWatcher = CreateEventWatcher(
                    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3",
                    OnSerialPortRemoved
                );

                // 2) Yeni bir seri port eklendiğinde izler
                _serialPortsAddedWatcher = CreateEventWatcher(
                    "SELECT * FROM __InstanceOperationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_SerialPort'",
                    OnSerialPortAdded
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing event watchers: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Belirtilen WQL sorgusuna göre bir ManagementEventWatcher oluşturur ve eventHandler'a bağlar.
        /// </summary>
        private ManagementEventWatcher CreateEventWatcher(string query, EventArrivedEventHandler eventHandler)
        {
            var watcher = new ManagementEventWatcher(new ManagementScope("root\\CIMV2"), new WqlEventQuery(query));
            watcher.EventArrived += eventHandler;
            watcher.Start();
            return watcher;
        }

        /// <summary>
        /// Fiziksel bir seri port çıkarıldığında tetiklenen eventin callback'i.
        /// Burada sadece "ScanSerialPorts" metodunu çağırarak listeyi güncelliyoruz.
        /// </summary>
        private void OnSerialPortRemoved(object sender, EventArrivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ScanSerialPorts);
        }

        /// <summary>
        /// Yeni bir seri port eklendiğinde tetiklenen eventin callback'i.
        /// Burada da "ScanSerialPorts" çağırıyoruz.
        /// </summary>
        private void OnSerialPortAdded(object sender, EventArrivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(ScanSerialPorts);
        }

        /// <summary>
        /// Sistemdeki seri portları tarar, "AvailablePorts" listesini günceller,
        /// yeni eklenen veya çıkarılan portlar için event fırlatır (SerialPortAdded/SerialPortRemoved).
        /// </summary>
        public void ScanSerialPorts()
        {
            try
            {
                // Mevcut "AvailablePorts" listesini hafızaya al
                var existingPorts = AvailablePorts.ToList();
                // Sistemden anlık port listesini çek
                var currentPorts = SerialPort.GetPortNames().ToList();

                // Yeni eklenen portlar
                foreach (var port in currentPorts.Except(existingPorts))
                {
                    AvailablePorts.Add(port);
                    SerialPortAdded?.Invoke(port);
                }

                // Kaldırılan portlar
                foreach (var port in existingPorts.Except(currentPorts))
                {
                    AvailablePorts.Remove(port);
                    SerialPortRemoved?.Invoke(port);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning serial ports: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// IDisposable arayüzü gereği Dispose metodu. Tüm bağlı portları kapatır
        /// ve event watcher'ları durdurur.
        /// </summary>
        public void Dispose()
        {
            // Önce tüm bağlı portları kopar
            foreach (var portName in ConnectedPorts.Select(p => p.PortName).ToList())
            {
                DisconnectFromPort(portName);
            }

            // WMI watcher'larını dispose et
            DisposeWatcher(_serialPortsRemovedWatcher);
            DisposeWatcher(_serialPortsAddedWatcher);
        }

        /// <summary>
        /// Bir ManagementEventWatcher'ın güvenle durdurulması ve dispose edilmesi için yardımcı metot.
        /// </summary>
        private void DisposeWatcher(ManagementEventWatcher watcher)
        {
            try
            {
                watcher?.Stop();
                watcher?.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disposing watcher: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion
    }
}
