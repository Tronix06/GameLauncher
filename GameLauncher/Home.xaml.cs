#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameLauncher
{
    // Estructura simple para amigos
    public class Amigo { public string Nombre; public string Estado; public Brush ColorEstado; }

    public partial class Home : Window
    {
        // --- NUEVO: GUARDAMOS EL USUARIO ACTUAL (EL PASAPORTE) ---
        private Usuario usuarioActual;

        private List<Juego> biblioteca = new List<Juego>();
        private bool esVistaCuadricula = false;
        private string criterioOrden = "Nombre";
        private string seccionActual = "TIENDA";

        private Juego juegoSeleccionado;
        private Juego juegoDescargandoActualmente;

        // TIMERS
        private DispatcherTimer timerCarrusel;
        private DispatcherTimer timerDescarga;
        private int indiceCarrusel = 0;

        // COLORES
        private Brush colorActivo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0074E0"));
        private Brush colorDorado = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
        private Brush colorVerde = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
        private Brush colorRojo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        private Brush colorTransparente = Brushes.Transparent;
        private Brush textoBlanco = Brushes.White;
        private Brush textoGris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));

        // --- CONSTRUCTOR ACTUALIZADO: RECIBE EL OBJETO USUARIO ---
        public Home(Usuario usuario)
        {
            InitializeComponent();
            this.usuarioActual = usuario; // Guardamos quien ha entrado

            ConfigurarVentanaInicial();
            CargarDatosFalsos();
            CargarAmigos();

            // Ponemos su nombre en la barra de arriba
            if (txtNombrePerfil != null) txtNombrePerfil.Text = usuarioActual.Nombre;

            // --- SEGURIDAD VISUAL (RBAC) ---
            // Aquí decidimos si el botón de Admin aparece o no
            ConfigurarPermisosRol();

            // Iniciar Carrusel
            timerCarrusel = new DispatcherTimer();
            timerCarrusel.Interval = TimeSpan.FromSeconds(5);
            timerCarrusel.Tick += (s, e) => RotarCarrusel();
            timerCarrusel.Start();
            RotarCarrusel();

            // EVENTO CLAVE: Si cambia el tamaño de la ventana, redibujamos para ajustar anchos
            this.SizeChanged += (s, e) =>
            {
                if (!esVistaCuadricula) RefrescarInterfaz();
            };

            RefrescarInterfaz();
        }

        // Constructor por defecto (por si acaso) crea un usuario invitado
        public Home() : this(new Usuario { Nombre = "Invitado", Rol = "usuario" }) { }

        // --- LÓGICA DE ROLES ---
        private void ConfigurarPermisosRol()
        {
            // Solo si es ADMIN mostramos el botón dorado
            if (usuarioActual != null && usuarioActual.EsAdmin())
            {
                if (btnAdmin != null) btnAdmin.Visibility = Visibility.Visible;
            }
            else
            {
                if (btnAdmin != null) btnAdmin.Visibility = Visibility.Collapsed;
            }
        }

        // --- EVENTO DEL BOTÓN ADMIN ---
        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            // AHORA PASAMOS EL USUARIO ACTUAL AL CONSTRUCTOR
            AdminWindow adminWindow = new AdminWindow(usuarioActual);
            adminWindow.ShowDialog();
        }

        private void ConfigurarVentanaInicial()
        {
            double alto = SystemParameters.PrimaryScreenHeight;
            double ancho = SystemParameters.PrimaryScreenWidth;
            this.Height = alto * 0.85;
            this.Width = ancho * 0.85;
            this.Left = (ancho - this.Width) / 2;
            this.Top = (alto - this.Height) / 2;
        }

        private void CargarDatosFalsos()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string Ruta(string carpeta, string archivo) => System.IO.Path.Combine(baseDir, "Assets", carpeta, archivo);

            biblioteca.Add(new Juego { Titulo = "CyberWar 2077", Genero = "RPG", EstaInstalado = true, Descripcion = "Futuro distópico y neón.", RutaImagen = Ruta("Portadas", "cyberwar_cover.png"), RutaBanner = Ruta("Banners", "cyberwar_banner.png"), Tamano = "70 GB", Version = "v2.1", Puntuacion = 5, HorasJugadas = 124 });
            biblioteca.Add(new Juego { Titulo = "Fantasy Realm", Genero = "MMORPG", EstaInstalado = false, Descripcion = "Dragones y magia.", RutaImagen = Ruta("Portadas", "fantasy_cover.png"), RutaBanner = Ruta("Banners", "fantasy_banner.png"), Tamano = "120 GB", Version = "v10.0", Puntuacion = 4, HorasJugadas = 0 });
            biblioteca.Add(new Juego { Titulo = "Space Shooter X", Genero = "Arcade", EstaInstalado = true, Descripcion = "Combates espaciales.", RutaImagen = Ruta("Portadas", "space_cover.png"), RutaBanner = Ruta("Banners", "space_banner.png"), Tamano = "15 GB", Version = "v1.5", Puntuacion = 3, HorasJugadas = 12 });
            biblioteca.Add(new Juego { Titulo = "Racing Legends", Genero = "Simulación", EstaInstalado = false, Descripcion = "Velocidad realista.", RutaImagen = Ruta("Portadas", "racing_cover.png"), RutaBanner = Ruta("Banners", "racing_banner.png"), Tamano = "85 GB", Version = "v4.2", Puntuacion = 5, HorasJugadas = 45 });
            biblioteca.Add(new Juego { Titulo = "Zombie Survival", Genero = "Terror", EstaInstalado = true, Descripcion = "Apocalipsis zombie.", RutaImagen = Ruta("Portadas", "zombie_cover.png"), RutaBanner = Ruta("Banners", "zombie_banner.png"), Tamano = "40 GB", Version = "v0.9", Puntuacion = 4, HorasJugadas = 8 });
            biblioteca.Add(new Juego { Titulo = "Puzzle Master", Genero = "Puzzle", EstaInstalado = false, Descripcion = "Misterio.", RutaImagen = Ruta("Portadas", "puzzle_cover.png"), RutaBanner = Ruta("Banners", "puzzle_banner.png"), Tamano = "2 GB", Version = "v1.0", Puntuacion = 3, HorasJugadas = 2 });
        }

        private void CargarAmigos()
        {
            if (ListaAmigos == null) return;
            var amigos = new List<Amigo> {
                new Amigo{ Nombre = "SniperWolf", Estado = "Jugando a CyberWar", ColorEstado = colorVerde },
                new Amigo{ Nombre = "AlexTheKid", Estado = "En línea", ColorEstado = colorActivo },
                new Amigo{ Nombre = "NoobMaster69", Estado = "Ausente", ColorEstado = Brushes.Orange },
                new Amigo{ Nombre = "SarahConnor", Estado = "Desconectado", ColorEstado = Brushes.Gray }
            };

            foreach (var a in amigos)
            {
                StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 10, 0, 10) };
                sp.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = a.ColorEstado, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
                StackPanel info = new StackPanel();
                info.Children.Add(new TextBlock { Text = a.Nombre, Foreground = Brushes.White, FontWeight = FontWeights.Bold });
                info.Children.Add(new TextBlock { Text = a.Estado, Foreground = Brushes.Gray, FontSize = 10 });
                sp.Children.Add(info);
                ListaAmigos.Children.Add(sp);
            }
        }

        private void RotarCarrusel()
        {
            if (biblioteca.Count == 0) return;
            indiceCarrusel = (indiceCarrusel + 1) % biblioteca.Count;
            var juego = biblioteca[indiceCarrusel];
            try { imgHero.Source = new BitmapImage(new Uri(juego.RutaBanner, UriKind.Absolute)); } catch { }
            txtHeroTitulo.Text = juego.Titulo.ToUpper();
            txtHeroDesc.Text = juego.Descripcion;
        }

        private void RefrescarInterfaz()
        {
            if (ContenedorJuegos == null) return;
            ContenedorJuegos.Children.Clear();

            if (seccionActual == "TIENDA") PanelHero.Visibility = Visibility.Visible;
            else PanelHero.Visibility = Visibility.Collapsed;

            var lista = biblioteca.ToList();

            if (seccionActual == "BIBLIOTECA")
            {
                lista = lista.Where(j => j.EstaInstalado).ToList();
                if (criterioOrden == "Instalado") { criterioOrden = "Nombre"; ActualizarEstiloBotonesSort(); }
            }

            if (txtBuscador != null && !string.IsNullOrEmpty(txtBuscador.Text))
                lista = lista.Where(j => j.Titulo.ToLower().Contains(txtBuscador.Text.ToLower())).ToList();

            if (criterioOrden == "Nombre") lista = lista.OrderBy(j => j.Titulo).ToList();
            else lista = lista.OrderByDescending(j => j.EstaInstalado).ToList();

            foreach (var juego in lista)
            {
                if (esVistaCuadricula) DibujarTarjetaGrid(juego);
                else DibujarFilaLista(juego);
            }
        }

        private void AccionJuego(Juego juego)
        {
            if (juego.EstaInstalado)
            {
                MessageBox.Show($"Iniciando {juego.Titulo}...\n\n¡A jugar!", "BiTronix", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                if (juegoDescargandoActualmente != null) { MessageBox.Show("Ya hay una descarga en curso.", "Espera", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (MessageBox.Show($"¿Descargar {juego.Titulo}?", "Instalar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    IniciarSimulacionDescarga(juego);
                }
            }
        }

        private void IniciarSimulacionDescarga(Juego juego)
        {
            juegoDescargandoActualmente = juego;
            PanelDescarga.Visibility = Visibility.Visible;
            txtEstadoDescarga.Text = $"DESCARGANDO {juego.Titulo.ToUpper()}...";
            pbDescarga.Value = 0;

            timerDescarga = new DispatcherTimer();
            timerDescarga.Interval = TimeSpan.FromMilliseconds(50);
            timerDescarga.Tick += (s, e) => {
                pbDescarga.Value += 1;
                txtPorcentajeDescarga.Text = $"{pbDescarga.Value}%";
                if (pbDescarga.Value >= 100)
                {
                    timerDescarga.Stop();
                    PanelDescarga.Visibility = Visibility.Hidden;
                    juego.EstaInstalado = true;
                    juegoDescargandoActualmente = null;
                    MessageBox.Show($"{juego.Titulo} instalado correctamente.", "Descarga Completada", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefrescarInterfaz();
                    if (OverlayDetalle.Visibility == Visibility.Visible) AbrirOverlay(juego);
                }
            };
            timerDescarga.Start();
        }

        // --- VISTA LISTA ---
        private void DibujarFilaLista(Juego juego)
        {
            // CÁLCULO DINÁMICO DEL ANCHO: Se ajusta al espacio real disponible
            double ancho = ContenedorJuegos.ActualWidth > 0 ? ContenedorJuegos.ActualWidth - 25 : 800;

            Border tarjeta = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515")), CornerRadius = new CornerRadius(5), Margin = new Thickness(0, 0, 0, 10), Width = ancho, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")), BorderThickness = new Thickness(1) };

            // Menú contextual
            tarjeta.ContextMenu = CrearMenuContextual(juego);

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid cabecera = new Grid { Background = Brushes.Transparent, Cursor = Cursors.Hand };
            Grid detalle = new Grid { Visibility = Visibility.Collapsed, Height = 220, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#101010")) };
            cabecera.MouseLeftButtonDown += (s, e) => detalle.Visibility = (detalle.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;

            StackPanel info = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10) };
            try { info.Children.Add(new Image { Source = new BitmapImage(new Uri(juego.RutaImagen, UriKind.Absolute)), Width = 50, Height = 50, Margin = new Thickness(0, 0, 15, 0), Stretch = Stretch.UniformToFill }); } catch { }
            StackPanel textos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textos.Children.Add(new TextBlock { Text = juego.Titulo, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 18 });
            string estrellas = new string('★', juego.Puntuacion) + new string('☆', 5 - juego.Puntuacion);
            StackPanel subtextos = new StackPanel { Orientation = Orientation.Horizontal };
            subtextos.Children.Add(new TextBlock { Text = juego.Genero + "  |  ", Foreground = Brushes.Gray, FontSize = 12 });
            subtextos.Children.Add(new TextBlock { Text = estrellas, Foreground = colorDorado, FontSize = 12 });
            if (juego.EstaInstalado && juego.HorasJugadas > 0) subtextos.Children.Add(new TextBlock { Text = $"  |  Played: {juego.HorasJugadas}h", Foreground = colorActivo, FontSize = 12, FontWeight = FontWeights.Bold });

            textos.Children.Add(subtextos);
            info.Children.Add(textos);
            TextBlock estado = new TextBlock { Text = juego.EstaInstalado ? "INSTALADO" : "NO INSTALADO", Foreground = juego.EstaInstalado ? colorVerde : Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 20, 0), FontWeight = FontWeights.Bold, FontSize = 11 };
            cabecera.Children.Add(info); cabecera.Children.Add(estado);

            try { detalle.Children.Add(new Image { Source = new BitmapImage(new Uri(juego.RutaBanner, UriKind.Absolute)), Stretch = Stretch.UniformToFill, Opacity = 0.3 }); } catch { }
            StackPanel contenidoDetalle = new StackPanel { Margin = new Thickness(75, 20, 20, 20), VerticalAlignment = VerticalAlignment.Center };
            contenidoDetalle.Children.Add(new TextBlock { Text = "SINOPSIS", Foreground = Brushes.Gray, FontSize = 10, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            contenidoDetalle.Children.Add(new TextBlock { Text = juego.Descripcion, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Width = 600, HorizontalAlignment = HorizontalAlignment.Left, FontSize = 14, Margin = new Thickness(0, 0, 0, 20) });
            StackPanel stats = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
            stats.Children.Add(CrearStatSimple("VERSIÓN", juego.Version)); stats.Children.Add(CrearStatSimple("TAMAÑO", juego.Tamano));
            contenidoDetalle.Children.Add(stats);
            StackPanel acciones = new StackPanel { Orientation = Orientation.Horizontal };
            Button btnAccion = CrearBotonAccion(juego.EstaInstalado ? "JUGAR" : "INSTALAR", true);
            btnAccion.Click += (s, e) => AccionJuego(juego);
            acciones.Children.Add(btnAccion);
            if (juego.EstaInstalado)
            {
                Button btnDes = CrearBotonAccion("DESINSTALAR", false);
                btnDes.Click += (s, e) => {
                    if (MessageBox.Show($"¿Eliminar {juego.Titulo} de la biblioteca?", "Desinstalar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        juego.EstaInstalado = false; RefrescarInterfaz();
                    }
                };
                acciones.Children.Add(btnDes);
            }
            contenidoDetalle.Children.Add(acciones);
            detalle.Children.Add(contenidoDetalle);
            Grid.SetRow(cabecera, 0); Grid.SetRow(detalle, 1);
            grid.Children.Add(cabecera); grid.Children.Add(detalle);
            tarjeta.Child = grid;
            ContenedorJuegos.Children.Add(tarjeta);
        }

        // --- VISTA GRID ---
        private void DibujarTarjetaGrid(Juego juego)
        {
            Border tarjeta = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181818")), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 15, 15), Width = 180, Height = 290, Cursor = Cursors.Hand, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")), BorderThickness = new Thickness(1) };
            tarjeta.ContextMenu = CrearMenuContextual(juego);
            tarjeta.MouseLeftButtonDown += (s, e) => AbrirOverlay(juego);
            tarjeta.MouseEnter += (s, e) => tarjeta.BorderBrush = colorActivo;
            tarjeta.MouseLeave += (s, e) => tarjeta.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

            Grid areaImagen = new Grid();
            try
            {
                Border mask = new Border { CornerRadius = new CornerRadius(8, 8, 0, 0), ClipToBounds = true, Background = Brushes.Black };
                mask.Child = new Image { Source = new BitmapImage(new Uri(juego.RutaImagen, UriKind.Absolute)), Stretch = Stretch.UniformToFill };
                areaImagen.Children.Add(mask);
            }
            catch { }
            Border badge = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(6, 2, 6, 2) };
            TextBlock txtBadge = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 10 };
            if (juego.EstaInstalado) { badge.Background = colorVerde; txtBadge.Text = "INSTALADO"; } else { badge.Background = colorActivo; txtBadge.Text = juego.Genero.ToUpper(); }
            badge.Child = txtBadge; areaImagen.Children.Add(badge);
            Grid.SetRow(areaImagen, 0); grid.Children.Add(areaImagen);

            StackPanel info = new StackPanel { Margin = new Thickness(10, 8, 10, 5), VerticalAlignment = VerticalAlignment.Top };
            info.Children.Add(new TextBlock { Text = juego.Titulo, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = juego.Titulo });
            string estrellas = new string('★', juego.Puntuacion) + new string('☆', 5 - juego.Puntuacion);
            info.Children.Add(new TextBlock { Text = estrellas, Foreground = colorDorado, FontSize = 12, Margin = new Thickness(0, 2, 0, 5) });
            Grid meta = new Grid(); meta.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); meta.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            TextBlock txtTam = new TextBlock { Text = juego.Tamano, Foreground = Brushes.Gray, FontSize = 10 };
            TextBlock txtEstado = new TextBlock { Text = "●", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, Foreground = juego.EstaInstalado ? colorVerde : Brushes.Gray };
            Grid.SetColumn(txtEstado, 1); meta.Children.Add(txtTam); meta.Children.Add(txtEstado);
            info.Children.Add(meta);
            Grid.SetRow(info, 1); grid.Children.Add(info);
            tarjeta.Child = grid; ContenedorJuegos.Children.Add(tarjeta);
        }

        private void AbrirOverlay(Juego juego)
        {
            juegoSeleccionado = juego;
            txtOverlayTitulo.Text = juego.Titulo.ToUpper(); txtOverlayGenero.Text = juego.Genero.ToUpper(); txtOverlayVersion.Text = juego.Version; txtOverlayDesc.Text = juego.Descripcion;
            try { imgOverlayBanner.Source = new BitmapImage(new Uri(juego.RutaBanner, UriKind.Absolute)); } catch { }

            btnOverlayAccion.Click -= BtnAccionOverlay_Click;
            btnOverlayAccion.Click += BtnAccionOverlay_Click;

            if (juego.EstaInstalado)
            {
                txtOverlayEstado.Text = "INSTALADO"; txtOverlayEstado.Foreground = colorVerde;
                btnOverlayAccion.Content = "JUGAR"; btnOverlayAccion.Background = colorActivo;
            }
            else
            {
                txtOverlayEstado.Text = "NO INSTALADO"; txtOverlayEstado.Foreground = Brushes.Gray;
                btnOverlayAccion.Content = "INSTALAR"; btnOverlayAccion.Background = Brushes.Gray;
            }
            OverlayDetalle.Visibility = Visibility.Visible;
        }

        private ContextMenu CrearMenuContextual(Juego juego)
        {
            ContextMenu menu = new ContextMenu();
            menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));
            menu.Foreground = Brushes.White;
            MenuItem itemJugar = new MenuItem { Header = juego.EstaInstalado ? "JUGAR" : "INSTALAR", FontWeight = FontWeights.Bold };
            itemJugar.Click += (s, e) => AccionJuego(juego);
            menu.Items.Add(itemJugar);
            menu.Items.Add(new Separator { Background = Brushes.Gray });
            MenuItem itemFav = new MenuItem { Header = "Añadir a Favoritos" };
            itemFav.Click += (s, e) => MessageBox.Show($"{juego.Titulo} añadido a favoritos.", "Favoritos", MessageBoxButton.OK, MessageBoxImage.Information);
            menu.Items.Add(itemFav);
            if (juego.EstaInstalado)
            {
                MenuItem itemDes = new MenuItem { Header = "Desinstalar", Foreground = colorRojo };
                itemDes.Click += (s, e) => { if (MessageBox.Show($"¿Desinstalar {juego.Titulo}?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { juego.EstaInstalado = false; RefrescarInterfaz(); } };
                menu.Items.Add(itemDes);
            }
            return menu;
        }

        // --- BOTÓN TOGGLE AMIGOS (CORREGIDO) ---
        private void BtnToggleFriends_Click(object sender, RoutedEventArgs e)
        {
            if (PanelAmigos.Visibility == Visibility.Visible)
                PanelAmigos.Visibility = Visibility.Collapsed;
            else
                PanelAmigos.Visibility = Visibility.Visible;

            // TRUCO: Forzamos redibujado para que el ancho de la lista se adapte
            // Usamos Dispatcher para que ocurra después de que el panel se oculte
            Dispatcher.BeginInvoke(new Action(() => RefrescarInterfaz()), DispatcherPriority.ContextIdle);
        }

        // OTROS EVENTOS
        private void BtnAccionOverlay_Click(object sender, RoutedEventArgs e) { if (juegoSeleccionado != null) AccionJuego(juegoSeleccionado); }
        private void BtnCerrarOverlay_Click(object sender, RoutedEventArgs e) { OverlayDetalle.Visibility = Visibility.Collapsed; OverlayPerfil.Visibility = Visibility.Collapsed; OverlayAjustes.Visibility = Visibility.Collapsed; OverlayWIP.Visibility = Visibility.Collapsed; }
        private void BtnCerrarOverlay_Click(object sender, MouseButtonEventArgs e) { OverlayDetalle.Visibility = Visibility.Collapsed; OverlayPerfil.Visibility = Visibility.Collapsed; OverlayAjustes.Visibility = Visibility.Collapsed; OverlayWIP.Visibility = Visibility.Collapsed; }
        private void BtnAbrirPerfil_Click(object sender, RoutedEventArgs e) { txtEditUser.Text = txtNombrePerfil.Text; OverlayPerfil.Visibility = Visibility.Visible; }
        private void BtnGuardarPerfil_Click(object sender, RoutedEventArgs e) { if (string.IsNullOrWhiteSpace(txtEditUser.Text)) { MessageBox.Show("Nombre vacío.", "Error"); return; } if (!string.IsNullOrEmpty(txtEditPassNew.Password)) { if (string.IsNullOrEmpty(txtEditPassOld.Password)) { MessageBox.Show("Falta contraseña actual.", "Error"); return; } if (txtEditPassNew.Password != txtEditPassConfirm.Password) { MessageBox.Show("Contraseñas no coinciden.", "Error"); return; } MessageBox.Show("Contraseña actualizada."); } txtNombrePerfil.Text = txtEditUser.Text; MessageBox.Show("Perfil guardado."); OverlayPerfil.Visibility = Visibility.Collapsed; txtEditPassOld.Clear(); txtEditPassNew.Clear(); txtEditPassConfirm.Clear(); }
        private void CambiarAvatar_Click(object sender, RoutedEventArgs e) { Button btn = sender as Button; if (btn != null) { if (btn.Name == "btnAvatarAzul") bordeAvatar.BorderBrush = new SolidColorBrush(Colors.Cyan); if (btn.Name == "btnAvatarRojo") bordeAvatar.BorderBrush = new SolidColorBrush(Colors.Red); if (btn.Name == "btnAvatarVerde") bordeAvatar.BorderBrush = new SolidColorBrush(Colors.LimeGreen); if (btn.Name == "btnAvatarMorado") bordeAvatar.BorderBrush = new SolidColorBrush(Colors.Purple); } }
        private void TogglePassOld_Click(object sender, RoutedEventArgs e) { if (txtEditPassOldVisible.Visibility == Visibility.Collapsed) { txtEditPassOldVisible.Text = txtEditPassOld.Password; txtEditPassOldVisible.Visibility = Visibility.Visible; txtEditPassOld.Visibility = Visibility.Collapsed; } else { txtEditPassOld.Password = txtEditPassOldVisible.Text; txtEditPassOld.Visibility = Visibility.Visible; txtEditPassOldVisible.Visibility = Visibility.Collapsed; } }
        private void TogglePassNew_Click(object sender, RoutedEventArgs e) { if (txtEditPassNewVisible.Visibility == Visibility.Collapsed) { txtEditPassNewVisible.Text = txtEditPassNew.Password; txtEditPassNewVisible.Visibility = Visibility.Visible; txtEditPassNew.Visibility = Visibility.Collapsed; } else { txtEditPassNew.Password = txtEditPassNewVisible.Text; txtEditPassNew.Visibility = Visibility.Visible; txtEditPassNewVisible.Visibility = Visibility.Collapsed; } }
        private void TogglePassConfirm_Click(object sender, RoutedEventArgs e) { if (txtEditPassConfirmVisible.Visibility == Visibility.Collapsed) { txtEditPassConfirmVisible.Text = txtEditPassConfirm.Password; txtEditPassConfirmVisible.Visibility = Visibility.Visible; txtEditPassConfirm.Visibility = Visibility.Collapsed; } else { txtEditPassConfirm.Password = txtEditPassConfirmVisible.Text; txtEditPassConfirm.Visibility = Visibility.Visible; txtEditPassConfirmVisible.Visibility = Visibility.Collapsed; } }
        private void BtnAbrirAjustes_Click(object sender, RoutedEventArgs e) => OverlayAjustes.Visibility = Visibility.Visible;
        private void BtnMenuTienda_Click(object sender, RoutedEventArgs e) { seccionActual = "TIENDA"; lblSeccionActual.Text = "TIENDA"; btnMenuTienda.Foreground = Brushes.White; btnMenuTienda.BorderThickness = new Thickness(4, 0, 0, 0); btnMenuBiblioteca.Foreground = textoGris; btnMenuBiblioteca.BorderThickness = new Thickness(0); btnSortInstalado.IsEnabled = true; btnSortInstalado.Opacity = 1; RefrescarInterfaz(); }
        private void BtnMenuBiblioteca_Click(object sender, RoutedEventArgs e) { seccionActual = "BIBLIOTECA"; lblSeccionActual.Text = "BIBLIOTECA"; btnMenuBiblioteca.Foreground = Brushes.White; btnMenuBiblioteca.BorderThickness = new Thickness(4, 0, 0, 0); btnMenuTienda.Foreground = textoGris; btnMenuTienda.BorderThickness = new Thickness(0); btnSortInstalado.IsEnabled = false; btnSortInstalado.Opacity = 0.3; RefrescarInterfaz(); }
        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e) => RefrescarInterfaz();
        private void BtnSortNombre_Click(object sender, RoutedEventArgs e) { criterioOrden = "Nombre"; ActualizarEstiloBotonesSort(); RefrescarInterfaz(); }
        private void BtnSortInstalado_Click(object sender, RoutedEventArgs e) { criterioOrden = "Instalado"; ActualizarEstiloBotonesSort(); RefrescarInterfaz(); }
        private void ActualizarEstiloBotonesSort() { if (criterioOrden == "Nombre") { btnSortNombre.Background = colorActivo; btnSortNombre.Foreground = textoBlanco; btnSortInstalado.Background = colorTransparente; btnSortInstalado.Foreground = textoGris; } else { btnSortInstalado.Background = colorActivo; btnSortInstalado.Foreground = textoBlanco; btnSortNombre.Background = colorTransparente; btnSortNombre.Foreground = textoGris; } }
        private void BtnVistaLista_Click(object sender, RoutedEventArgs e) { esVistaCuadricula = false; if (ContenedorJuegos != null) ContenedorJuegos.Orientation = Orientation.Horizontal; if (btnVistaLista != null) { btnVistaLista.Foreground = textoBlanco; btnVistaGrid.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")); } RefrescarInterfaz(); }
        private void BtnVistaGrid_Click(object sender, RoutedEventArgs e) { esVistaCuadricula = true; RefrescarInterfaz(); if (btnVistaGrid != null) { btnVistaGrid.Foreground = textoBlanco; btnVistaLista.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")); } }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void BtnMinimizar_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnMaximizar_Click(object sender, RoutedEventArgs e) => this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void BtnSalir_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }

        // REDIBUJAR AL CAMBIAR TAMAÑO DE VENTANA (RESPONSIVE)
        protected override void OnRender(DrawingContext drawingContext) { base.OnRender(drawingContext); if (!esVistaCuadricula) RefrescarInterfaz(); }
        private void WIP_Click(object sender, RoutedEventArgs e) => OverlayWIP.Visibility = Visibility.Visible;

        // Helpers
        private StackPanel CrearStatSimple(string titulo, string valor) { StackPanel sp = new StackPanel { Margin = new Thickness(0, 0, 30, 0) }; sp.Children.Add(new TextBlock { Text = titulo, Foreground = Brushes.Gray, FontSize = 10, FontWeight = FontWeights.Bold }); sp.Children.Add(new TextBlock { Text = valor, Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.Bold }); return sp; }
        private Button CrearBotonAccion(string texto, bool esPrincipal) { Button btn = new Button { Content = texto, Width = 120, Height = 35, Margin = new Thickness(0, 0, 10, 0), Cursor = Cursors.Hand, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0) }; if (esPrincipal) { btn.Background = colorActivo; btn.Foreground = Brushes.White; } else { btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")); btn.Foreground = Brushes.White; } ControlTemplate template = new ControlTemplate(typeof(Button)); FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border)); border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty)); border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4)); FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter)); content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center); content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center); border.AppendChild(content); template.VisualTree = border; btn.Template = template; return btn; }
    }
}