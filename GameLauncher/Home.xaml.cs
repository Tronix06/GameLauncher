#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using System.IO;
// LIBRERÍAS IA
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace GameLauncher
{
    // Clase auxiliar para la lista de amigos
    public class Amigo { public string Nombre; public string Estado; public Brush ColorEstado; }

    public partial class Home : Window
    {
        private Usuario usuarioActual;
        private DatabaseConnection db = new DatabaseConnection();

        private List<Juego> listaTienda = new List<Juego>();
        private List<Juego> listaBiblioteca = new List<Juego>();

        private bool esVistaCuadricula = true;
        private string criterioOrden = "Nombre";
        private string seccionActual = "TIENDA";

        private Juego juegoSeleccionado;
        private DispatcherTimer timerCarrusel;
        private DispatcherTimer timerDescarga;
        private int indiceCarrusel = 0;

        // --- CONFIGURACIÓN IA (GROQ) ---
        // ⚠️ PEGA AQUÍ TU CLAVE DE GROQ (Empieza por 'gsk_')
        private const string API_KEY = "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";

        private const string API_URL = "https://api.groq.com/openai/v1/chat/completions";

        // Colores
        private Brush colorActivo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0074E0"));
        private Brush colorVerde = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
        private Brush colorDorado = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
        private Brush colorRojo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        private Brush textoGris = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
        private Brush textoBlanco = Brushes.White;

        public Home(Usuario usuario)
        {
            InitializeComponent();
            this.usuarioActual = usuario;

            ConfigurarVentanaInicial();

            CargarTiendaDesdeBBDD();
            CargarBibliotecaDesdeBBDD();
            CargarAmigos();

            if (txtNombrePerfil != null) txtNombrePerfil.Text = usuarioActual.Nombre;
            ConfigurarPermisosRol();

            timerCarrusel = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timerCarrusel.Tick += (s, e) => RotarCarrusel();
            timerCarrusel.Start();
            RotarCarrusel();

            this.SizeChanged += (s, e) => { if (!esVistaCuadricula) RefrescarInterfaz(); };
            RefrescarInterfaz();
        }

        public Home() : this(new Usuario { Nombre = "Invitado", Rol = "usuario", Id = 0 }) { }

        // --- MÉTODO IA ACTUALIZADO (MODELO NUEVO) ---
        // --- MÉTODO IA ACTUALIZADO (HISTORIA LARGA + VENTANA PROPIA) ---
        // --- MÉTODO IA ACTUALIZADO (DETECTA SI EL JUEGO ES REAL O INVENTADO) ---
        private async void BtnGenerarLore_Click(object sender, RoutedEventArgs e)
        {
            if (juegoSeleccionado == null) return;

            // 1. Interfaz visual
            txtTituloLore.Text = juegoSeleccionado.Titulo.ToUpper();
            txtLoreContent.Text = "📡 Analizando base de datos global...\n\nDeterminando si es un juego clásico o un universo nuevo...";
            OverlayLore.Visibility = Visibility.Visible;
            btnGenerarLore.IsEnabled = false;

            try
            {
                // 2. EL PROMPT PERFECTO (HÍBRIDO: REALIDAD VS FICCIÓN)
                string prompt = $@"
                    Actúa como un experto Historiador de Videojuegos y Diseñador Narrativo.
                    Analiza los siguientes datos de un videojuego:
                    
                    TÍTULO: '{juegoSeleccionado.Titulo}'
                    GÉNERO: '{juegoSeleccionado.Genero}'
                    DESCRIPCIÓN DEL USUARIO: ""{juegoSeleccionado.Descripcion}""

                    TU TAREA:
                    Primero, determina si este es un juego real famoso (como 'Tres en Raya', 'Tetris', 'Minecraft', 'Super Mario') o un proyecto ficticio desconocido.

                    CASO A: SI EL JUEGO ES REAL/CLÁSICO:
                    No inventes ficción absurda. Escribe sobre su origen real, su lógica matemática o su impacto histórico.
                    - ORÍGENES: Cuándo se creó o cuál es su origen histórico/matemático.
                    - EL OBJETIVO: Cuál es el conflicto lógico o meta real del juego.
                    - LEGADO: Por qué es conocido o cuál es su atmósfera real.

                    CASO B: SI EL JUEGO PARECE INVENTADO/DESCONOCIDO:
                    Usa la descripción del usuario para inventar un 'Lore' (trasfondo) creativo, dramático e inmersivo.
                    - ORÍGENES: Inventa cómo empezó ese mundo.
                    - EL CONFLICTO: Facciones, villanos o problemas dramáticos.
                    - ATMÓSFERA: Describe el ambiente inventado.

                    SALIDA OBLIGATORIA (Sin introducciones, directo al texto):
                    1. CONTEXTO: (Tu respuesta aquí)
                    2. DINÁMICA/CONFLICTO: (Tu respuesta aquí)
                    3. AMBIENTACIÓN: (Tu respuesta aquí)
                    
                    Escribe unas 150-200 palabras en total.";

                var requestBody = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[] { new { role = "user", content = prompt } }
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", API_KEY.Trim());
                    HttpResponseMessage response = await client.PostAsync(API_URL, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(responseString))
                        {
                            string historiaGenerada = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            txtLoreContent.Text = historiaGenerada.Trim();
                        }
                    }
                    else
                    {
                        txtLoreContent.Text = "❌ Error: La IA no pudo analizar el juego.";
                    }
                }
            }
            catch (Exception ex)
            {
                txtLoreContent.Text = $"❌ Error crítico: {ex.Message}";
            }
            finally
            {
                btnGenerarLore.IsEnabled = true;
            }
        }

        // --- CARGA DE AMIGOS ---
        private void CargarAmigos()
        {
            if (ListaAmigos == null) return;
            ListaAmigos.Children.Clear();
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

        // --- BBDD: CARGAR TIENDA ---
        private void CargarTiendaDesdeBBDD()
        {
            listaTienda.Clear();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "SELECT * FROM videojuegos WHERE es_visible = TRUE";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaTienda.Add(new Juego
                            {
                                Id = reader.GetInt32("id"),
                                Titulo = reader.GetString("titulo"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "" : reader.GetString("descripcion"),
                                Precio = reader.GetDecimal("precio"),
                                RutaImagen = ResolverRutaImagen(reader.IsDBNull(reader.GetOrdinal("imagen_url")) ? "" : reader.GetString("imagen_url"), "Portadas"),
                                RutaBanner = ResolverRutaImagen(reader.IsDBNull(reader.GetOrdinal("banner_url")) ? "" : reader.GetString("banner_url"), "Banners"),
                                Genero = reader.IsDBNull(reader.GetOrdinal("genero")) ? "" : reader.GetString("genero"),
                                FechaLanzamiento = reader.IsDBNull(reader.GetOrdinal("fecha_lanzamiento")) ? (DateTime?)null : reader.GetDateTime("fecha_lanzamiento"),
                                EsVisible = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error tienda: " + ex.Message); }
        }

        // --- BBDD: CARGAR BIBLIOTECA ---
        private void CargarBibliotecaDesdeBBDD()
        {
            listaBiblioteca.Clear();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = @"SELECT v.*, b.horas_jugadas, b.esta_instalado, b.es_favorito 
                                     FROM biblioteca b 
                                     JOIN videojuegos v ON b.videojuego_id = v.id 
                                     WHERE b.usuario_id = @uid";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@uid", usuarioActual.Id);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaBiblioteca.Add(new Juego
                            {
                                Id = reader.GetInt32("id"),
                                Titulo = reader.GetString("titulo"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "" : reader.GetString("descripcion"),
                                RutaImagen = ResolverRutaImagen(reader.IsDBNull(reader.GetOrdinal("imagen_url")) ? "" : reader.GetString("imagen_url"), "Portadas"),
                                RutaBanner = ResolverRutaImagen(reader.IsDBNull(reader.GetOrdinal("banner_url")) ? "" : reader.GetString("banner_url"), "Banners"),
                                Genero = reader.IsDBNull(reader.GetOrdinal("genero")) ? "" : reader.GetString("genero"),
                                FechaLanzamiento = reader.IsDBNull(reader.GetOrdinal("fecha_lanzamiento")) ? (DateTime?)null : reader.GetDateTime("fecha_lanzamiento"),
                                HorasJugadas = reader.GetDouble("horas_jugadas"),
                                EstaInstalado = reader.GetBoolean("esta_instalado"),
                                EsFavorito = reader.GetBoolean("es_favorito")
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error biblioteca: " + ex.Message); }
        }

        // --- COMPRAR JUEGO ---
        private void ComprarJuego(Juego juego)
        {
            if (BiTronixMsgBox.Show($"¿Confirmar compra de {juego.Titulo} por {juego.Precio:C}?", "Pasarela de Pago", BiTronixMsgBox.Type.Confirmation, BiTronixMsgBox.Buttons.YesNo))
            {
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        string query = "INSERT INTO biblioteca (usuario_id, videojuego_id, esta_instalado, horas_jugadas) VALUES (@uid, @jid, 0, 0)";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@uid", usuarioActual.Id);
                        cmd.Parameters.AddWithValue("@jid", juego.Id);
                        cmd.ExecuteNonQuery();
                    }
                    BiTronixMsgBox.Show("¡Juego añadido a tu biblioteca!", "Compra Exitosa", BiTronixMsgBox.Type.Success);
                    CargarBibliotecaDesdeBBDD();
                    RefrescarInterfaz();
                    if (OverlayDetalle.Visibility == Visibility.Visible) AbrirOverlay(juego);
                }
                catch (Exception ex) { BiTronixMsgBox.Show("Error compra: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
            }
        }

        // --- INSTALAR JUEGO ---
        private void IniciarSimulacionDescarga(Juego juego)
        {
            PanelDescarga.Visibility = Visibility.Visible;
            txtEstadoDescarga.Text = $"DESCARGANDO {juego.Titulo.ToUpper()}...";
            pbDescarga.Value = 0;
            timerDescarga = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            timerDescarga.Tick += (s, e) =>
            {
                pbDescarga.Value += 1;
                txtPorcentajeDescarga.Text = $"{pbDescarga.Value}%";
                if (pbDescarga.Value >= 100)
                {
                    timerDescarga.Stop();
                    PanelDescarga.Visibility = Visibility.Hidden;
                    try
                    {
                        using (var conn = db.GetConnection())
                        {
                            string query = "UPDATE biblioteca SET esta_instalado = 1 WHERE usuario_id = @uid AND videojuego_id = @jid";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@uid", usuarioActual.Id);
                            cmd.Parameters.AddWithValue("@jid", juego.Id);
                            cmd.ExecuteNonQuery();
                        }
                        juego.EstaInstalado = true;
                        BiTronixMsgBox.Show($"{juego.Titulo} instalado y listo.", "Completado", BiTronixMsgBox.Type.Success);
                        RefrescarInterfaz();
                        if (OverlayDetalle.Visibility == Visibility.Visible) AbrirOverlay(juego);
                    }
                    catch { }
                }
            };
            timerDescarga.Start();
        }

        // --- DESINSTALAR JUEGO ---
        private void DesinstalarJuego(Juego juego)
        {
            if (MessageBox.Show($"¿Estás seguro de que quieres desinstalar {juego.Titulo}?\nSe mantendrán tus horas jugadas.", "Desinstalar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        string query = "UPDATE biblioteca SET esta_instalado = 0 WHERE usuario_id = @uid AND videojuego_id = @jid";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@uid", usuarioActual.Id);
                        cmd.Parameters.AddWithValue("@jid", juego.Id);
                        cmd.ExecuteNonQuery();
                    }
                    juego.EstaInstalado = false;
                    BiTronixMsgBox.Show($"{juego.Titulo} se ha desinstalado.", "Operación Correcta", BiTronixMsgBox.Type.Info);
                    if (juegoSeleccionado == juego && OverlayDetalle.Visibility == Visibility.Visible) AbrirOverlay(juego);
                    RefrescarInterfaz();
                }
                catch (Exception ex) { BiTronixMsgBox.Show("Error al desinstalar: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
            }
        }

        // --- JUGAR JUEGO ---
        private void JugarJuego(Juego juego)
        {
            BiTronixMsgBox.Show($"Lanzando {juego.Titulo}...\n¡Disfruta!", "BiTronix", BiTronixMsgBox.Type.Info);
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "UPDATE biblioteca SET horas_jugadas = horas_jugadas + 1 WHERE usuario_id = @uid AND videojuego_id = @jid";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@uid", usuarioActual.Id);
                    cmd.Parameters.AddWithValue("@jid", juego.Id);
                    cmd.ExecuteNonQuery();
                }
                juego.HorasJugadas++;
                RefrescarInterfaz();
            }
            catch { }
        }

        // --- REFRESCO DE INTERFAZ ---
        private void RefrescarInterfaz()
        {
            if (ContenedorJuegos == null) return;
            ContenedorJuegos.Children.Clear();
            PanelHero.Visibility = seccionActual == "TIENDA" ? Visibility.Visible : Visibility.Collapsed;
            var listaA_Mostrar = seccionActual == "TIENDA" ? listaTienda : listaBiblioteca;
            if (!string.IsNullOrEmpty(txtBuscador.Text)) listaA_Mostrar = listaA_Mostrar.Where(j => j.Titulo.ToLower().Contains(txtBuscador.Text.ToLower())).ToList();
            if (criterioOrden == "Nombre") listaA_Mostrar = listaA_Mostrar.OrderBy(j => j.Titulo).ToList();
            foreach (var juego in listaA_Mostrar)
            {
                if (esVistaCuadricula) DibujarTarjetaGrid(juego);
                else DibujarFilaLista(juego);
            }
        }

        // --- MENÚ CONTEXTUAL ---
        private ContextMenu CrearMenuContextual(Juego juego)
        {
            ContextMenu menu = new ContextMenu();
            menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515"));
            menu.Foreground = Brushes.White;
            menu.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

            MenuItem itemAccion = new MenuItem { Header = juego.EstaInstalado ? "▶ JUGAR" : "⬇ INSTALAR", FontWeight = FontWeights.Bold };
            itemAccion.Click += (s, e) => { if (juego.EstaInstalado) JugarJuego(juego); else IniciarSimulacionDescarga(juego); };
            menu.Items.Add(itemAccion);

            menu.Items.Add(new Separator { Background = Brushes.Gray });

            if (juego.EstaInstalado)
            {
                MenuItem itemDes = new MenuItem { Header = "🗑️ Desinstalar", Foreground = colorRojo };
                itemDes.Click += (s, e) => DesinstalarJuego(juego);
                menu.Items.Add(itemDes);
            }

            MenuItem itemDetalles = new MenuItem { Header = "ℹ️ Ver Detalles" };
            itemDetalles.Click += (s, e) => AbrirOverlay(juego);
            menu.Items.Add(itemDetalles);

            return menu;
        }

        // --- DIBUJAR TARJETA (GRID) ---
        private void DibujarTarjetaGrid(Juego juego)
        {
            bool enBiblioteca = listaBiblioteca.Any(b => b.Id == juego.Id);
            Border tarjeta = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181818")), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 15, 15), Width = 180, Height = 290, Cursor = Cursors.Hand, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")), BorderThickness = new Thickness(1) };
            if (enBiblioteca) tarjeta.ContextMenu = CrearMenuContextual(juego);
            tarjeta.MouseLeftButtonDown += (s, e) => AbrirOverlay(juego);
            tarjeta.MouseEnter += (s, e) => tarjeta.BorderBrush = colorActivo;
            tarjeta.MouseLeave += (s, e) => tarjeta.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });

            Grid areaImagen = new Grid();
            Border mask = new Border { CornerRadius = new CornerRadius(8, 8, 0, 0), ClipToBounds = true, Background = Brushes.Black };
            try { mask.Child = new Image { Source = new BitmapImage(new Uri(juego.RutaImagen, UriKind.RelativeOrAbsolute)), Stretch = Stretch.UniformToFill }; } catch { }
            areaImagen.Children.Add(mask);

            Border badge = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(5), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(6, 2, 6, 2) };
            TextBlock txtBadge = new TextBlock { Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 10 };

            if (seccionActual == "TIENDA")
            {
                if (enBiblioteca) { badge.Background = colorVerde; txtBadge.Text = "✓ EN BIBLIOTECA"; }
                else { badge.Background = colorActivo; txtBadge.Text = juego.Precio == 0 ? "GRATIS" : $"{juego.Precio} €"; }
            }
            else
            {
                badge.Background = juego.EstaInstalado ? colorVerde : Brushes.Gray;
                txtBadge.Text = juego.EstaInstalado ? "INSTALADO" : "NO INSTALADO";
            }

            badge.Child = txtBadge; areaImagen.Children.Add(badge);
            Grid.SetRow(areaImagen, 0); grid.Children.Add(areaImagen);

            StackPanel info = new StackPanel { Margin = new Thickness(10, 8, 10, 5) };
            info.Children.Add(new TextBlock { Text = juego.Titulo, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis });
            info.Children.Add(new TextBlock { Text = juego.Genero, Foreground = textoGris, FontSize = 12 });

            if (seccionActual == "BIBLIOTECA" && juego.HorasJugadas > 0)
            {
                info.Children.Add(new TextBlock { Text = $"⏳ {juego.HorasJugadas} h", Foreground = colorDorado, FontSize = 11, Margin = new Thickness(0, 2, 0, 0) });
            }

            Grid.SetRow(info, 1); grid.Children.Add(info);
            tarjeta.Child = grid; ContenedorJuegos.Children.Add(tarjeta);
        }

        // --- DIBUJAR FILA (LISTA) ---
        private void DibujarFilaLista(Juego juego)
        {
            bool enBiblioteca = listaBiblioteca.Any(b => b.Id == juego.Id);
            double ancho = ContenedorJuegos.ActualWidth > 0 ? ContenedorJuegos.ActualWidth - 25 : 800;
            Border tarjeta = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#151515")), CornerRadius = new CornerRadius(5), Margin = new Thickness(0, 0, 0, 10), Width = ancho, Height = 70, Cursor = Cursors.Hand };
            if (enBiblioteca) tarjeta.ContextMenu = CrearMenuContextual(juego);
            tarjeta.MouseLeftButtonDown += (s, e) => AbrirOverlay(juego);

            Grid grid = new Grid();
            StackPanel info = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10) };
            try { info.Children.Add(new Image { Source = new BitmapImage(new Uri(juego.RutaImagen, UriKind.RelativeOrAbsolute)), Width = 50, Height = 50, Margin = new Thickness(0, 0, 15, 0) }); } catch { }

            StackPanel textos = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textos.Children.Add(new TextBlock { Text = juego.Titulo, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 16 });
            string subtexto = juego.Genero;
            if (seccionActual == "BIBLIOTECA") subtexto += $"  |  ⏳ {juego.HorasJugadas} h";
            textos.Children.Add(new TextBlock { Text = subtexto, Foreground = textoGris, FontSize = 12 });
            info.Children.Add(textos);

            grid.Children.Add(info);

            TextBlock precio = new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 20, 0), FontWeight = FontWeights.Bold };
            if (seccionActual == "TIENDA")
            {
                precio.Text = enBiblioteca ? "✓" : (juego.Precio == 0 ? "GRATIS" : $"{juego.Precio} €");
                precio.Foreground = enBiblioteca ? colorVerde : colorDorado;
            }
            else
            {
                precio.Text = juego.EstaInstalado ? "▶ JUGAR" : "⬇ INSTALAR";
                precio.Foreground = juego.EstaInstalado ? colorVerde : Brushes.Gray;
            }
            grid.Children.Add(precio);
            tarjeta.Child = grid; ContenedorJuegos.Children.Add(tarjeta);
        }

        // --- OVERLAY DETALLES ---
        private void AbrirOverlay(Juego juego)
        {
            juegoSeleccionado = juego;
            bool enBiblioteca = listaBiblioteca.Any(b => b.Id == juego.Id);

            txtOverlayTitulo.Text = juego.Titulo.ToUpper();
            txtOverlayGenero.Text = juego.Genero;

            string fechaStr = juego.FechaLanzamiento.HasValue ? juego.FechaLanzamiento.Value.ToShortDateString() : "TBA";
            txtOverlayDesc.Text = $"📅 Lanzamiento: {fechaStr}\n\n{juego.Descripcion}";

            try { imgOverlayBanner.Source = new BitmapImage(new Uri(juego.RutaBanner, UriKind.RelativeOrAbsolute)); } catch { }

            // RESETEO DE ESTADO DEL BOTÓN DE IA
            btnGenerarLore.Content = "✨ GENERAR HISTORIA CON IA";
            btnGenerarLore.IsEnabled = true;
            btnGenerarLore.Visibility = Visibility.Visible;

            if (seccionActual == "TIENDA")
            {
                txtOverlayPrecio.Text = juego.Precio == 0 ? "GRATIS" : $"{juego.Precio:C}";
                if (enBiblioteca)
                {
                    btnOverlayAccion.Content = "EN BIBLIOTECA";
                    btnOverlayAccion.Background = Brushes.Gray;
                    btnOverlayAccion.IsEnabled = false;
                }
                else
                {
                    btnOverlayAccion.Content = "COMPRAR";
                    btnOverlayAccion.Background = colorVerde;
                    btnOverlayAccion.IsEnabled = true;
                }
            }
            else
            {
                txtOverlayPrecio.Text = "";
                if (juego.EstaInstalado)
                {
                    btnOverlayAccion.Content = "JUGAR";
                    btnOverlayAccion.Background = colorActivo;
                    btnOverlayAccion.IsEnabled = true;
                }
                else
                {
                    btnOverlayAccion.Content = "INSTALAR";
                    btnOverlayAccion.Background = Brushes.Gray;
                    btnOverlayAccion.IsEnabled = true;
                }
            }
            OverlayDetalle.Visibility = Visibility.Visible;
        }

        private void BtnAccionOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (juegoSeleccionado == null) return;
            if (seccionActual == "TIENDA") ComprarJuego(juegoSeleccionado);
            else { if (juegoSeleccionado.EstaInstalado) JugarJuego(juegoSeleccionado); else IniciarSimulacionDescarga(juegoSeleccionado); }
        }

        private string ResolverRutaImagen(string nombreArchivo, string subcarpeta)
        {
            if (string.IsNullOrEmpty(nombreArchivo)) return "";
            if (nombreArchivo.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return nombreArchivo;
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", subcarpeta, nombreArchivo);
        }

        private void RotarCarrusel()
        {
            if (listaTienda.Count == 0) return;
            indiceCarrusel = (indiceCarrusel + 1) % listaTienda.Count;
            var juego = listaTienda[indiceCarrusel];
            try { imgHero.Source = new BitmapImage(new Uri(juego.RutaBanner, UriKind.RelativeOrAbsolute)); } catch { }
            txtHeroTitulo.Text = juego.Titulo.ToUpper();
            txtHeroDesc.Text = juego.Descripcion;
        }

        // --- CONTROLADORES DE EVENTOS ---
        private void ConfigurarPermisosRol() { if (usuarioActual != null && usuarioActual.EsAdmin()) btnAdmin.Visibility = Visibility.Visible; else btnAdmin.Visibility = Visibility.Collapsed; }
        private void ConfigurarVentanaInicial() { this.Height = SystemParameters.PrimaryScreenHeight * 0.85; this.Width = SystemParameters.PrimaryScreenWidth * 0.85; this.Left = (SystemParameters.PrimaryScreenWidth - Width) / 2; this.Top = (SystemParameters.PrimaryScreenHeight - Height) / 2; }
        private void BtnAdmin_Click(object sender, RoutedEventArgs e) { new AdminWindow(usuarioActual).ShowDialog(); CargarTiendaDesdeBBDD(); RefrescarInterfaz(); }
        private void BtnMenuTienda_Click(object sender, RoutedEventArgs e) { seccionActual = "TIENDA"; lblSeccionActual.Text = "TIENDA"; btnSortInstalado.IsEnabled = false; RefrescarInterfaz(); }
        private void BtnMenuBiblioteca_Click(object sender, RoutedEventArgs e) { seccionActual = "BIBLIOTECA"; lblSeccionActual.Text = "BIBLIOTECA"; btnSortInstalado.IsEnabled = true; RefrescarInterfaz(); }
        private void WIP_Click(object sender, RoutedEventArgs e) => OverlayWIP.Visibility = Visibility.Visible;
        private void BtnCerrarOverlay_Click(object sender, RoutedEventArgs e) { OverlayDetalle.Visibility = Visibility.Collapsed; OverlayAjustes.Visibility = Visibility.Collapsed; OverlayPerfil.Visibility = Visibility.Collapsed; OverlayWIP.Visibility = Visibility.Collapsed; OverlayLore.Visibility = Visibility.Collapsed; }
        private void BtnCerrarOverlay_Click(object sender, MouseButtonEventArgs e) { OverlayDetalle.Visibility = Visibility.Collapsed; OverlayAjustes.Visibility = Visibility.Collapsed; OverlayPerfil.Visibility = Visibility.Collapsed; OverlayWIP.Visibility = Visibility.Collapsed; OverlayLore.Visibility = Visibility.Collapsed; }
        private void BtnVistaGrid_Click(object sender, RoutedEventArgs e) { esVistaCuadricula = true; RefrescarInterfaz(); }
        private void BtnVistaLista_Click(object sender, RoutedEventArgs e) { esVistaCuadricula = false; RefrescarInterfaz(); }
        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e) => RefrescarInterfaz();
        private void BtnSortNombre_Click(object sender, RoutedEventArgs e) { criterioOrden = "Nombre"; RefrescarInterfaz(); }
        private void BtnSortInstalado_Click(object sender, RoutedEventArgs e) { criterioOrden = "Instalado"; RefrescarInterfaz(); }
        private void BtnToggleFriends_Click(object sender, RoutedEventArgs e) { if (PanelAmigos.Visibility == Visibility.Visible) PanelAmigos.Visibility = Visibility.Collapsed; else PanelAmigos.Visibility = Visibility.Visible; Dispatcher.BeginInvoke(new Action(() => RefrescarInterfaz()), DispatcherPriority.ContextIdle); }
        private void BtnSalir_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void BtnMinimizar_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnMaximizar_Click(object sender, RoutedEventArgs e) => this.WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e) { new MainWindow().Show(); this.Close(); }
        private void BtnAbrirAjustes_Click(object sender, RoutedEventArgs e) => OverlayAjustes.Visibility = Visibility.Visible;
        private void BtnAbrirPerfil_Click(object sender, RoutedEventArgs e) => OverlayPerfil.Visibility = Visibility.Visible;
        private void BtnGuardarPerfil_Click(object sender, RoutedEventArgs e) { OverlayPerfil.Visibility = Visibility.Collapsed; }
        private void CambiarAvatar_Click(object sender, RoutedEventArgs e) { }
        private void TogglePassOld_Click(object sender, RoutedEventArgs e) { }
        private void TogglePassNew_Click(object sender, RoutedEventArgs e) { }
        private void TogglePassConfirm_Click(object sender, RoutedEventArgs e) { }
    }
}