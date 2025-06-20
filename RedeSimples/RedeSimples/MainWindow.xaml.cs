using Microsoft.Win32;
using Models;
using Models.Enum;
using Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace RedeSimples
{
    public class ToolboxItem
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string Tag { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly NetworkCommandService _networkService = new();
        private readonly List<NetworkCable> _networkCables = new();
        private readonly List<Room> _rooms = new();
        private readonly List<NetworkDevice> _devices = new();

        private FrameworkElement? _selectedElement;
        private NetworkDevice? _selectedDevice;
        private Point _mouseOffset;
        private Room? _selectedRoom;

        private AdornerLayer? _adornerLayer;
        private ResizingAdorner? _resizingAdorner;
        private ToolState _currentTool = ToolState.Select;
        private Cursor? _cableCursor;
        private Point? _cableStartPoint;
        private NetworkCable? _currentNetworkCable;
        private Line? _previewCableLine;

        private string? _currentFilePath;
        private int _nextDeviceId = 1;


        public MainWindow()
        {
            InitializeComponent();
            LoadToolbox();
            LoadCableCursor();
            MainCanvas.DragOver += MainCanvas_DragOver;
            MainCanvas.MouseLeftButtonDown += MainCanvas_MouseLeftButtonDown;
            MainCanvas.MouseMove += MainCanvas_MouseMove;
            MainCanvas.MouseRightButtonDown += MainCanvas_MouseRightButtonDown;
        }

        #region Salvar e Abrir

        private void Abrir_Click(object sender, RoutedEventArgs e)
        {
            if (_devices.Any() || _rooms.Any() || _networkCables.Any())
            {
                var result = MessageBox.Show("Tem certeza que deseja abrir um novo arquivo? Todo o progresso não salvo será perdido.", "Confirmar Abertura", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Arquivos de Rede Simples (*.xml)|*.xml|Todos os arquivos (*.*)|*.*",
                Title = "Abrir Projeto de Rede"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadFromFile(openFileDialog.FileName);
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SalvarComo_Click(sender, e);
            }
            else
            {
                SaveChanges(_currentFilePath);
            }
        }

        private void SalvarComo_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivos de Rede Simples (*.xml)|*.xml",
                Title = "Salvar Projeto de Rede",
                FileName = "MeuProjetoDeRede.xml"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                _currentFilePath = saveFileDialog.FileName;
                SaveChanges(_currentFilePath);
            }
        }

        private void SaveChanges(string filePath)
        {
            try
            {
                var state = new CanvasState
                {
                    Devices = _devices,
                    Rooms = _rooms,
                    Cables = _networkCables
                };

                foreach (var cable in state.Cables)
                {
                    cable.ConnectedDeviceIds = cable.ConnectedDevices.Select(d => d.Id).ToList();
                }

                var serializer = new XmlSerializer(typeof(CanvasState));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, state);
                }
                MessageBox.Show("Projeto salvo com sucesso!", "Salvar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro ao salvar o arquivo: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}", "Erro de Salvamento", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("O arquivo selecionado não existe.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ClearCanvas();

                var serializer = new XmlSerializer(typeof(CanvasState));
                CanvasState state;
                using (var reader = new StreamReader(filePath))
                {
                    state = (CanvasState)serializer.Deserialize(reader);
                }

                _devices.AddRange(state.Devices);
                _rooms.AddRange(state.Rooms);
                _networkCables.AddRange(state.Cables);

                _nextDeviceId = _devices.Any() ? _devices.Max(d => d.Id) + 1 : 1;

                var deviceLookup = _devices.ToDictionary(d => d.Id);

                foreach (var room in _rooms) CreateRoomVisual(room);
                foreach (var device in _devices) CreateDeviceVisual(device);

                foreach (var cable in _networkCables)
                {
                    cable.ConnectedDevices = cable.ConnectedDeviceIds
                        .Select(id => deviceLookup.ContainsKey(id) ? deviceLookup[id] : null)
                        .Where(d => d != null)
                        .ToList();

                    foreach (var segment in cable.Cables)
                    {
                        CreateCableVisual(segment);
                    }
                }

                _currentFilePath = filePath;
                MessageBox.Show("Projeto carregado com sucesso!", "Abrir", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro ao carregar o arquivo: {ex.Message}", "Erro de Carregamento", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCanvas()
        {
            MainCanvas.Children.Clear();
            _devices.Clear();
            _rooms.Clear();
            _networkCables.Clear();
            _selectedElement = null;
            _selectedDevice = null;
            _selectedRoom = null;
            _currentFilePath = null;
            _nextDeviceId = 1;

            if (_resizingAdorner != null)
            {
                _adornerLayer?.Remove(_resizingAdorner);
                _resizingAdorner = null;
            }

            PropertiesPanel.Visibility = Visibility.Collapsed;
            RoomPropertiesPanel.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Passar Cabo
        private void LoadCableCursor()
        {
            string cursorPath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\cursor.cur";
            if (File.Exists(cursorPath))
            {
                _cableCursor = new Cursor(cursorPath);
            }
            else
            {
                MessageBox.Show("Arquivo do cursor não encontrado: " + cursorPath);
            }
        }

        private void BtnPassarCabo_Click(object sender, RoutedEventArgs e)
        {
            _currentTool = ToolState.DrawingCable;
            MainCanvas.Cursor = _cableCursor;
            if (_resizingAdorner != null)
            {
                _adornerLayer?.Remove(_resizingAdorner);
                _resizingAdorner = null;
            }
            _selectedElement = null;
            PropertiesPanel.Visibility = Visibility.Collapsed;
            RoomPropertiesPanel.Visibility = Visibility.Collapsed;
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentTool == ToolState.DrawingCable && _cableStartPoint.HasValue)
            {
                Point currentPos = e.GetPosition(MainCanvas);
                if (_previewCableLine != null)
                {
                    MainCanvas.Children.Remove(_previewCableLine);
                }
                _previewCableLine = new Line
                {
                    X1 = _cableStartPoint.Value.X,
                    Y1 = _cableStartPoint.Value.Y,
                    X2 = currentPos.X,
                    Y2 = currentPos.Y,
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 3, 2 }
                };
                MainCanvas.Children.Add(_previewCableLine);
            }
            else if (_currentTool == ToolState.Select)
            {
                Element_MouseMove(sender, e);
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == MainCanvas)
            {
                if (_currentTool == ToolState.Select)
                {
                    if (_resizingAdorner != null)
                    {
                        _adornerLayer?.Remove(_resizingAdorner);
                        _resizingAdorner = null;
                    }
                    _selectedElement = null;
                    _selectedDevice = null;
                    _selectedRoom = null;
                    PropertiesPanel.Visibility = Visibility.Collapsed;
                    RoomPropertiesPanel.Visibility = Visibility.Collapsed;
                }
            }

            switch (_currentTool)
            {
                case ToolState.DrawingCable:
                    HandleCableDrawing_MouseDown(e);
                    break;
            }
        }

        private void HandleCableDrawing_MouseDown(MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(MainCanvas);
            NetworkDevice? hitDevice = null;

            HitTestResult hitTestResult = VisualTreeHelper.HitTest(MainCanvas, clickPosition);
            if (hitTestResult != null)
            {
                var frameworkElement = FindVisualParent<ContentControl>(hitTestResult.VisualHit);
                if (frameworkElement?.Tag is NetworkDevice device)
                {
                    hitDevice = device;
                    clickPosition = GetElementCenter(frameworkElement);
                }
            }

            if (!_cableStartPoint.HasValue)
            {
                _cableStartPoint = clickPosition;
                _currentNetworkCable = new NetworkCable();

                if (hitDevice != null)
                {
                    _currentNetworkCable.ConnectedDevices.Add(hitDevice);
                }
            }
            else
            {
                var newCableSegment = new Models.Cable
                {
                    StartX = (int)_cableStartPoint.Value.X,
                    StartY = (int)_cableStartPoint.Value.Y,
                    EndX = (int)clickPosition.X,
                    EndY = (int)clickPosition.Y,
                    Size = 2
                };

                _currentNetworkCable.Cables.Add(newCableSegment);
                CreateCableVisual(newCableSegment);

                _cableStartPoint = clickPosition;

                if (hitDevice != null)
                {
                    _currentNetworkCable.ConnectedDevices.Add(hitDevice);
                    FinalizeCableDrawing();
                }
            }
        }

        private void MainCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentTool == ToolState.DrawingCable)
            {
                FinalizeCableDrawing();
            }
        }

        private void FinalizeCableDrawing()
        {
            if (_currentNetworkCable != null && _currentNetworkCable.Cables.Any())
            {
                _networkCables.Add(_currentNetworkCable);
            }

            _currentTool = ToolState.Select;
            MainCanvas.Cursor = Cursors.Arrow;
            _cableStartPoint = null;
            _currentNetworkCable = null;

            if (_previewCableLine != null)
            {
                MainCanvas.Children.Remove(_previewCableLine);
                _previewCableLine = null;
            }
        }

        private void CreateCableVisual(Models.Cable cable)
        {
            var line = new Line
            {
                X1 = cable.StartX,
                Y1 = cable.StartY,
                X2 = cable.EndX,
                Y2 = cable.EndY,
                Stroke = (Brush)new BrushConverter().ConvertFromString(cable.Color),
                StrokeThickness = cable.Size
            };

            MainCanvas.Children.Add(line);
            Canvas.SetZIndex(line, 1);
        }
        #endregion


        private T? FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T t) return t;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private Point GetElementCenter(FrameworkElement element)
        {
            double x = Canvas.GetLeft(element) + element.Width / 2;
            double y = Canvas.GetTop(element) + element.Height / 2;
            return new Point(x, y);
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentTool == ToolState.Select && _selectedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(MainCanvas);
                double newX = mousePos.X - _mouseOffset.X;
                double newY = mousePos.Y - _mouseOffset.Y;

                Canvas.SetLeft(_selectedElement, newX);
                Canvas.SetTop(_selectedElement, newY);

                if (_selectedDevice != null)
                {
                    _selectedDevice.Rectangle.X = (int)newX;
                    _selectedDevice.Rectangle.Y = (int)newY;
                }
                else if (_selectedRoom != null)
                {
                    _selectedRoom.Rectangle.X = (int)newX;
                    _selectedRoom.Rectangle.Y = (int)newY;
                    _selectedRoom.Rectangle.Width = (int)_selectedElement.Width;
                    _selectedRoom.Rectangle.Height = (int)_selectedElement.Height;
                }
            }
        }

        private void LoadToolbox()
        {
            var toolboxItems = new List<ToolboxItem>
            {
                new ToolboxItem { Name = "Computador", ImagePath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\pc.png", Tag = "PC" },
                new ToolboxItem { Name = "Roteador", ImagePath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\router.png", Tag = "Router" },
                new ToolboxItem { Name = "Switch", ImagePath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\switch.png", Tag = "Switch" },
                new ToolboxItem { Name = "Impressora", ImagePath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\printer.png", Tag = "Printer" },
                new ToolboxItem { Name = "Cômodo", ImagePath = "C:\\Users\\walte\\Downloads\\room.png", Tag = "Room" }
            };
            Toolbox.ItemsSource = toolboxItems;
        }

        #region Drag-and-Drop
        private void Toolbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ItemsControl itemsControl)
            {
                var container = itemsControl.ContainerFromElement((DependencyObject)e.OriginalSource) as FrameworkElement;
                if (container?.DataContext is ToolboxItem toolboxItem)
                {
                    DragDrop.DoDragDrop(container, toolboxItem.Tag, DragDropEffects.Copy);
                }
            }
        }

        private void MainCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void MainCanvas_Drop(object sender, DragEventArgs e)
        {
            string? deviceTag = e.Data.GetData(DataFormats.StringFormat) as string;
            if (deviceTag == null) return;

            Point dropPosition = e.GetPosition(MainCanvas);
            NetworkDevice? newDevice = null;

            switch (deviceTag)
            {
                case "PC": newDevice = new PC { Name = "Novo PC", IP = "192.168.0.10" }; break;
                case "Router": newDevice = new Router { Name = "Roteador", IP = "192.168.0.1" }; break;
                case "Switch": newDevice = new Switch { Name = "Switch", IP = "192.168.0.2" }; break;
                case "Printer": newDevice = new Printer { Name = "Impressora", IP = "192.168.0.50" }; break;
                case "Room":
                    var newRoom = new Room { Name = "Novo Cômodo", Rectangle = new Models.Rectangle((int)dropPosition.X, (int)dropPosition.Y, 200, 150) };
                    _rooms.Add(newRoom);
                    CreateRoomVisual(newRoom);
                    return;
            }

            if (newDevice != null)
            {
                newDevice.Id = _nextDeviceId++;
                newDevice.Rectangle = new Models.Rectangle((int)dropPosition.X, (int)dropPosition.Y, 50, 50);
                _devices.Add(newDevice);
                CreateDeviceVisual(newDevice);
            }
        }
        #endregion

        #region Visual Element Creation
        private void CreateDeviceVisual(NetworkDevice device)
        {
            var image = new Image { Source = GetImageSourceForDevice(device), Width = 50, Height = 50 };
            var content = new ContentControl
            {
                Width = device.Rectangle.Width,
                Height = device.Rectangle.Height,
                Content = image,
                Tag = device,
            };
            Canvas.SetLeft(content, device.Rectangle.X);
            Canvas.SetTop(content, device.Rectangle.Y);
            content.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            content.MouseMove += Element_MouseMove;
            content.MouseLeftButtonUp += Element_MouseLeftButtonUp;
            content.ToolTip = device.Name;
            MainCanvas.Children.Add(content);
            Canvas.SetZIndex(content, 2);
        }

        private void CreateRoomVisual(Room room)
        {
            var backgroundBrush = (Brush)new BrushConverter().ConvertFromString(room.BackgroundColor);
            var borderBrush = (Brush)new BrushConverter().ConvertFromString(room.BorderColor);
            var border = new Border
            {
                Width = room.Rectangle.Width,
                Height = room.Rectangle.Height,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(room.BorderThickness),
                Tag = room,
                Background = backgroundBrush
            };
            var textBlock = new TextBlock { Text = room.Name, Foreground = new SolidColorBrush(Colors.Black), Margin = new Thickness(5) };
            border.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            border.MouseMove += Element_MouseMove;
            border.MouseLeftButtonUp += Element_MouseLeftButtonUp;
            var roomCanvas = new Canvas();
            roomCanvas.Children.Add(textBlock);
            border.Child = roomCanvas;
            Canvas.SetLeft(border, room.Rectangle.X);
            Canvas.SetTop(border, room.Rectangle.Y);
            MainCanvas.Children.Add(border);
        }

        private ImageSource GetImageSourceForDevice(NetworkDevice device)
        {
            string basePath = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\";
            string path = device switch
            {
                PC => basePath + "pc.png",
                Router => basePath + "router.png",
                Switch => basePath + "switch.png",
                Printer => basePath + "printer.png",
                _ => ""
            };
            return new BitmapImage(new Uri(path, UriKind.Absolute));
        }
        #endregion

        #region Element Selection and Movement
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_resizingAdorner != null)
            {
                _adornerLayer?.Remove(_resizingAdorner);
                _resizingAdorner = null;
            }

            _selectedElement = sender as FrameworkElement;
            if (_selectedElement != null)
            {
                _selectedDevice = null;
                _selectedRoom = null;

                if (_selectedElement.Tag is Room selectedRoom)
                {
                    _selectedRoom = selectedRoom;
                    UpdateRoomPropertiesPanel();
                    _adornerLayer = AdornerLayer.GetAdornerLayer(_selectedElement);
                    if (_adornerLayer != null)
                    {
                        _resizingAdorner = new ResizingAdorner(_selectedElement);
                        _adornerLayer.Add(_resizingAdorner);
                    }
                }
                else if (_selectedElement.Tag is NetworkDevice selectedDevice)
                {
                    _selectedDevice = selectedDevice;
                    UpdatePropertiesPanel();
                }

                _mouseOffset = e.GetPosition(_selectedElement);
                _selectedElement.CaptureMouse();
            }
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _selectedElement?.ReleaseMouseCapture();
        }
        #endregion

        #region Properties Panels
        private void UpdateRoomPropertiesPanel()
        {
            if (_selectedRoom != null)
            {
                RoomPropertiesPanel.Visibility = Visibility.Visible;
                PropertiesPanel.Visibility = Visibility.Collapsed;
                TxtRoomName.Text = _selectedRoom.Name;
                TxtRoomBackgroundColor.Text = _selectedRoom.BackgroundColor;
                TxtRoomBorderColor.Text = _selectedRoom.BorderColor;
                SliderBorderThickness.Value = _selectedRoom.BorderThickness;
            }
            else
            {
                RoomPropertiesPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtRoomName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedRoom != null && _selectedElement is Border border && border.Child is Canvas innerCanvas && innerCanvas.Children.Count > 0 && innerCanvas.Children[0] is TextBlock textBlock)
            {
                _selectedRoom.Name = TxtRoomName.Text;
                textBlock.Text = _selectedRoom.Name;
            }
        }

        private void TxtRoomBackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedRoom != null && _selectedElement is Border border)
            {
                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(TxtRoomBackgroundColor.Text);
                    border.Background = brush;
                    _selectedRoom.BackgroundColor = TxtRoomBackgroundColor.Text;
                }
                catch { /* Ignora cores inválidas */ }
            }
        }

        private void TxtRoomBorderColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedRoom != null && _selectedElement is Border border)
            {
                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(TxtRoomBorderColor.Text);
                    border.BorderBrush = brush;
                    _selectedRoom.BorderColor = TxtRoomBorderColor.Text;
                }
                catch { /* Ignora cores inválidas */ }
            }
        }

        private void SliderBorderThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_selectedRoom != null && _selectedElement is Border border)
            {
                _selectedRoom.BorderThickness = e.NewValue;
                border.BorderThickness = new Thickness(e.NewValue);
            }
        }

        private void UpdatePropertiesPanel()
        {
            if (_selectedDevice != null)
            {
                PropertiesPanel.Visibility = Visibility.Visible;
                RoomPropertiesPanel.Visibility = Visibility.Collapsed;
                TxtName.Text = _selectedDevice.Name;
                TxtIpAddress.Text = _selectedDevice.IP;
            }
            else
            {
                PropertiesPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedDevice != null)
            {
                _selectedDevice.Name = TxtName.Text;
                if (_selectedElement is FrameworkElement element)
                {
                    element.ToolTip = _selectedDevice.Name;
                }
            }
        }

        private void TxtIpAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedDevice != null) _selectedDevice.IP = TxtIpAddress.Text;
        }

        private async void BtnPing_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice != null && !string.IsNullOrWhiteSpace(_selectedDevice.IP))
            {
                TxtOutput.Text = $"Executando ping para {_selectedDevice.IP}...";
                string result = await _networkService.PingAsync(_selectedDevice.IP);
                TxtOutput.Text = result;
            }
            else
            {
                TxtOutput.Text = "Selecione um dispositivo com um endereço IP válido para executar o ping.";
            }
        }
        #endregion

        #region TraceRoute
        private async void BtnTraceRoute_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null || string.IsNullOrWhiteSpace(_selectedDevice.IP))
            {
                TxtOutput.Text = "Selecione um dispositivo de destino com um IP válido.";
                return;
            }

            TxtOutput.Text = $"Executando traceroute para {_selectedDevice.IP}...";
            string traceResult = await _networkService.TraceRouteAsync(_selectedDevice.IP);
            TxtOutput.Text = traceResult;

            List<string> ipHops = ParseTraceRouteOutput(traceResult);
            if (!ipHops.Any())
            {
                TxtOutput.Text += "\n\nNão foi possível extrair a rota do traceroute.";
                return;
            }

            if (ipHops.LastOrDefault() != _selectedDevice.IP)
            {
                ipHops.Add(_selectedDevice.IP);
            }

            List<NetworkDevice> devicePath = new List<NetworkDevice>();
            foreach (var ip in ipHops)
            {
                var device = FindDeviceByIp(ip);
                if (device != null && devicePath.LastOrDefault() != device)
                {
                    devicePath.Add(device);
                }
            }

            if (devicePath.Count < 2)
            {
                TxtOutput.Text += "\n\nNão foram encontrados dispositivos suficientes no canvas para visualizar a rota.";
                return;
            }

            await AnimatePacket(devicePath);
        }

        private NetworkDevice? FindDeviceByIp(string ip)
        {
            return _devices.FirstOrDefault(d => d.IP == ip.Trim());
        }

        private List<string> ParseTraceRouteOutput(string output)
        {
            var ips = new List<string>();
            var ipRegex = new Regex(@"\s+((?:\d{1,3}\.){3}\d{1,3})", RegexOptions.Compiled);
            using var reader = new StringReader(output);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().StartsWith("Tracing")) continue;
                var match = ipRegex.Match(line);
                if (match.Success)
                {
                    ips.Add(match.Groups[1].Value);
                }
            }
            return ips;
        }

        private async Task AnimatePacket(List<NetworkDevice> path)
        {
            var packetVisual = new Ellipse { Width = 15, Height = 15, Fill = Brushes.LawnGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 2 };
            Canvas.SetZIndex(packetVisual, 99);
            MainCanvas.Children.Add(packetVisual);

            for (int i = 0; i < path.Count - 1; i++)
            {
                var sourceDevice = path[i];
                var destinationDevice = path[i + 1];
                Point startPoint = GetElementCenter(FindVisualByDevice(sourceDevice));
                Point endPoint = GetElementCenter(FindVisualByDevice(destinationDevice));

                Canvas.SetLeft(packetVisual, startPoint.X - packetVisual.Width / 2);
                Canvas.SetTop(packetVisual, startPoint.Y - packetVisual.Height / 2);
                packetVisual.Visibility = Visibility.Visible;

                var duration = TimeSpan.FromSeconds(1);
                var moveX = new DoubleAnimation(endPoint.X - packetVisual.Width / 2, duration);
                var moveY = new DoubleAnimation(endPoint.Y - packetVisual.Height / 2, duration);
                packetVisual.BeginAnimation(Canvas.LeftProperty, moveX);
                packetVisual.BeginAnimation(Canvas.TopProperty, moveY);
                await Task.Delay(duration);
            }
            MainCanvas.Children.Remove(packetVisual);
        }

        private FrameworkElement FindVisualByDevice(NetworkDevice device)
        {
            foreach (var child in MainCanvas.Children)
            {
                if (child is FrameworkElement element && element.Tag == device)
                {
                    return element;
                }
            }
            throw new Exception("Visual for device not found.");
        }
        #endregion
    }
}