using Models;
using Service;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RedeSimples
{
    // Classe auxiliar para os itens da caixa de ferramentas
    public class ToolboxItem
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public string Tag { get; set; } // Para identificar o tipo de dispositivo (ex: "PC", "Router")
    }

    public partial class MainWindow : Window
    {
        private readonly NetworkCommandService _networkService = new();

        private readonly List<Room> _rooms = new();
        private readonly List<NetworkDevice> _devices = new();

        private FrameworkElement? _selectedElement;
        private NetworkDevice? _selectedDevice;
        private Point _mouseOffset;
        private Room? _selectedRoom;

        private AdornerLayer? _adornerLayer;
        private ResizingAdorner? _resizingAdorner;

        public MainWindow()
        {
            InitializeComponent();
            LoadToolbox();
            MainCanvas.DragOver += MainCanvas_DragOver;
            MainCanvas.MouseLeftButtonDown += MainCanvas_MouseLeftButtonDown;
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

        // CORREÇÃO PRINCIPAL: Lógica robusta para iniciar o drag-and-drop
        private void Toolbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ItemsControl itemsControl)
            {
                // Encontra o container do item que foi clicado a partir da origem do evento
                var container = itemsControl.ContainerFromElement((DependencyObject)e.OriginalSource) as FrameworkElement;
                if (container?.DataContext is ToolboxItem toolboxItem)
                {
                    // Inicia o DragDrop com a Tag do item de dados (DataContext)
                    DragDrop.DoDragDrop(container, toolboxItem.Tag, DragDropEffects.Copy);
                }
            }
        }

        // Handler para permitir a operação de "soltar" no canvas
        private void MainCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
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
                case "PC":
                    newDevice = new PC { Name = "Novo PC", IP = "192.168.0.10" };
                    break;
                case "Router":
                    newDevice = new Router { Name = "Roteador", IP = "192.168.0.1" };
                    break;
                case "Switch":
                    newDevice = new Switch { Name = "Switch", IP = "192.168.0.2" };
                    break;
                case "Printer":
                    newDevice = new Printer { Name = "Impressora", IP = "192.168.0.50" };
                    break;
                case "Room":
                    var newRoom = new Room
                    {
                        Name = "Novo Cômodo",
                        Rectangle = new Models.Rectangle((int)dropPosition.X, (int)dropPosition.Y, 200, 150)
                    };
                    _rooms.Add(newRoom);
                    CreateRoomVisual(newRoom);
                    return;
            }

            if (newDevice != null)
            {
                newDevice.Rectangle = new Models.Rectangle((int)dropPosition.X, (int)dropPosition.Y, 50, 50);
                _devices.Add(newDevice);
                CreateDeviceVisual(newDevice);
            }
        }

        #endregion

        #region Visual Element Creation

        private void CreateDeviceVisual(NetworkDevice device)
        {
            var image = new Image
            {
                Source = GetImageSourceForDevice(device),
                Width = 50,
                Height = 50
            };

            var content = new ContentControl
            {
                Width = device.Rectangle.width,
                Height = device.Rectangle.height,
                Content = image,
                Tag = device,
            };

            Canvas.SetLeft(content, device.Rectangle.x);
            Canvas.SetTop(content, device.Rectangle.y);

            content.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            content.MouseMove += Element_MouseMove;
            content.MouseLeftButtonUp += Element_MouseLeftButtonUp;
            content.ToolTip = device.Name;

            MainCanvas.Children.Add(content);
        }

        private void CreateRoomVisual(Room room)
        {
            var backgroundBrush = (Brush)new BrushConverter().ConvertFromString(room.BackgroundColor);
            var borderBrush = (Brush)new BrushConverter().ConvertFromString(room.BorderColor);

            var border = new Border
            {
                Width = room.Rectangle.width,
                Height = room.Rectangle.height,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(room.BorderThickness),
                Tag = room,
                Background = backgroundBrush
            };

            var textBlock = new TextBlock
            {
                Text = room.Name,
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(5)
            };

            border.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            border.MouseMove += Element_MouseMove;
            border.MouseLeftButtonUp += Element_MouseLeftButtonUp;

            var roomCanvas = new Canvas();
            roomCanvas.Children.Add(textBlock);
            border.Child = roomCanvas;

            Canvas.SetLeft(border, room.Rectangle.x);
            Canvas.SetTop(border, room.Rectangle.y);

            MainCanvas.Children.Add(border);
        }

        // Correção para carregamento da imagem com caminho absoluto
        private ImageSource GetImageSourceForDevice(NetworkDevice device)
        {
            string Base = "D:\\Projects\\RedeSimples\\RedeSimples\\Assets\\";
            string path = "";
            if (device is PC) path = Base + "pc.png";
            if (device is Router) path = Base + "router.png";
            if (device is Switch) path = Base + "switch.png";
            if (device is Printer) path = Base + "printer.png";

            // Usar UriKind.Absolute para caminhos de arquivo completos
            return new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Absolute));
        }

        #endregion

        #region Drag-and-Drop Logic (Mover elementos no Canvas)

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Remove o adorner da seleção anterior, se existir
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

                if (_selectedElement.Tag is NetworkDevice selectedDevice)
                {
                    _selectedDevice = selectedDevice;
                    UpdatePropertiesPanel(); // Mostra e popula o painel do dispositivo
                }

                _mouseOffset = e.GetPosition(_selectedElement);
                _selectedElement.CaptureMouse();
            }
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Se o clique foi no próprio Canvas e não em um de seus filhos
            if (e.Source == MainCanvas)
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

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(MainCanvas);
                double newX = mousePos.X - _mouseOffset.X;
                double newY = mousePos.Y - _mouseOffset.Y;

                Canvas.SetLeft(_selectedElement, newX);
                Canvas.SetTop(_selectedElement, newY);

                if (_selectedDevice != null)
                {
                    _selectedDevice.Rectangle.x = (int)newX;
                    _selectedDevice.Rectangle.y = (int)newY;
                }
                else if (_selectedRoom != null)
                {
                    _selectedRoom.Rectangle.x = (int)newX;
                    _selectedRoom.Rectangle.y = (int)newY;
                    _selectedRoom.Rectangle.width = (int)_selectedElement.Width;
                    _selectedRoom.Rectangle.height = (int)_selectedElement.Height;
                }
            }
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null)
            {
                _selectedElement.ReleaseMouseCapture();
                _selectedElement = null;
            }
        }

        #endregion

        // Coloque esta seção de código dentro da classe MainWindow

        #region Room Properties Panel

        private void UpdateRoomPropertiesPanel()
        {
            if (_selectedRoom != null)
            {
                RoomPropertiesPanel.Visibility = Visibility.Visible;
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
            if (_selectedRoom != null && _selectedElement is Border border)
            {
                _selectedRoom.Name = TxtRoomName.Text;
                // Atualiza o texto dentro da borda
                if (border.Child is Canvas innerCanvas && innerCanvas.Children.Count > 0 && innerCanvas.Children[0] is TextBlock textBlock)
                {
                    textBlock.Text = _selectedRoom.Name;
                }
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
                catch
                {
                    // Ignora cores inválidas
                }
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
                catch
                {
                    // Ignora cores inválidas
                }
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

        #endregion

        #region Properties Panel and Network Commands

        private void UpdatePropertiesPanel()
        {
            if (_selectedDevice != null)
            {
                PropertiesPanel.Visibility = Visibility.Visible;
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
            if (_selectedDevice != null)
            {
                _selectedDevice.IP = TxtIpAddress.Text;
            }
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
    }
}