using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GameLauncher
{
    public partial class AdminWindow : Window
    {
        private List<Usuario> listaUsuarios = new List<Usuario>();
        private List<Juego> listaJuegos = new List<Juego>();
        private DatabaseConnection db = new DatabaseConnection();
        private Usuario usuarioOriginalSnapshot;
        private Usuario adminLogueado;

        public class ApelacionEntry
        {
            public int Id { get; set; }
            public int UsuarioId { get; set; }
            public string NombreUsuario { get; set; }
            public string Mensaje { get; set; }
            public string Estado { get; set; }
            public string Fecha { get; set; }
        }

        // --- CONSTRUCTOR: AQUÍ APLICAMOS LA RESTRICCIÓN ---
        public AdminWindow(Usuario admin)
        {
            InitializeComponent();
            this.adminLogueado = admin;

            // 1. SI NO PUEDE GESTIONAR USUARIOS (ej: Admin Juegos) -> OCULTAR TABS
            if (!admin.PuedeGestionarUsuarios())
            {
                TabUsuarios.Visibility = Visibility.Collapsed;
                TabApelaciones.Visibility = Visibility.Collapsed; // <--- AQUÍ SE OCULTA LA PESTAÑA APELACIONES
            }

            // 2. SI NO PUEDE GESTIONAR JUEGOS (ej: Admin Usuarios) -> OCULTAR TABS
            if (!admin.PuedeGestionarJuegos())
            {
                TabJuegos.Visibility = Visibility.Collapsed;
            }

            // Cargas condicionales para no gastar recursos
            if (admin.PuedeGestionarUsuarios())
            {
                CargarUsuarios();
                CargarApelaciones();
                VerificarNotificaciones();
            }

            if (admin.PuedeGestionarJuegos())
            {
                CargarJuegos();
            }

            CargarLogs(); // Logs visibles para todos

            txtAdminUser.Text = $" | Sesión iniciada como: {admin.Nombre} (ID: {admin.Id} - {admin.TipoAdmin})";
        }

        // --- MÉTODOS DE FOTOS (OPCIÓN C) ---
        private void BtnBuscarImagen_Click(object sender, RoutedEventArgs e) => txtImagenJuego.Text = SeleccionarYCopiarImagen("Portadas");
        private void BtnBuscarBanner_Click(object sender, RoutedEventArgs e) => txtBannerJuego.Text = SeleccionarYCopiarImagen("Banners");

        private string SeleccionarYCopiarImagen(string subcarpeta)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imágenes|*.jpg;*.png;*.jpeg;*.bmp";

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string nombreArchivo = System.IO.Path.GetFileName(ofd.FileName);
                    string carpetaDestino = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", subcarpeta);
                    if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
                    string rutaDestinoCompleta = System.IO.Path.Combine(carpetaDestino, nombreArchivo);
                    if (!File.Exists(rutaDestinoCompleta)) File.Copy(ofd.FileName, rutaDestinoCompleta);
                    return nombreArchivo; // Solo guardamos el nombre
                }
                catch (Exception ex) { BiTronixMsgBox.Show("Error copiando: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
            }
            return "";
        }

        // ==========================================
        // GESTIÓN DE JUEGOS
        // ==========================================
        private void CargarJuegos()
        {
            listaJuegos.Clear();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "SELECT * FROM videojuegos";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaJuegos.Add(new Juego
                            {
                                Id = reader.GetInt32("id"),
                                Titulo = reader.GetString("titulo"),
                                Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "" : reader.GetString("descripcion"),
                                Precio = reader.GetDecimal("precio"),
                                RutaImagen = reader.IsDBNull(reader.GetOrdinal("imagen_url")) ? "" : reader.GetString("imagen_url"),
                                RutaBanner = reader.IsDBNull(reader.GetOrdinal("banner_url")) ? "" : reader.GetString("banner_url"),
                                Genero = reader.IsDBNull(reader.GetOrdinal("genero")) ? "" : reader.GetString("genero"),
                                EsVisible = reader.GetBoolean("es_visible")
                            });
                        }
                    }
                }
                dgJuegos.ItemsSource = null;
                dgJuegos.ItemsSource = listaJuegos;
            }
            catch (Exception ex) { BiTronixMsgBox.Show("Error cargando catálogo: " + ex.Message, "Error BBDD", BiTronixMsgBox.Type.Error); }
        }

        private void DgJuegos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Juego j = (Juego)dgJuegos.SelectedItem;
            if (j != null)
            {
                txtIdJuego.Text = j.Id.ToString();
                txtTituloJuego.Text = j.Titulo;
                txtPrecioJuego.Text = j.Precio.ToString("0.00");
                cmbGeneroJuego.Text = j.Genero;
                txtImagenJuego.Text = j.RutaImagen;
                txtBannerJuego.Text = j.RutaBanner;
                txtDescJuego.Text = j.Descripcion;
                chkVisible.IsChecked = j.EsVisible;
                btnEliminarJuego.IsEnabled = true;
            }
        }

        private void BtnGuardarJuego_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTituloJuego.Text)) { BiTronixMsgBox.Show("El título es obligatorio.", "Falta Dato", BiTronixMsgBox.Type.Warning); return; }
            if (!decimal.TryParse(txtPrecioJuego.Text, out decimal precio)) { BiTronixMsgBox.Show("El precio debe ser un número válido.", "Formato Incorrecto", BiTronixMsgBox.Type.Warning); return; }

            try
            {
                using (var conn = db.GetConnection())
                {
                    string query;
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;

                    if (string.IsNullOrEmpty(txtIdJuego.Text)) // INSERTAR
                        query = "INSERT INTO videojuegos (titulo, descripcion, precio, genero, imagen_url, banner_url, es_visible) VALUES (@tit, @desc, @pre, @gen, @img, @ban, @vis)";
                    else // ACTUALIZAR
                    {
                        query = "UPDATE videojuegos SET titulo=@tit, descripcion=@desc, precio=@pre, genero=@gen, imagen_url=@img, banner_url=@ban, es_visible=@vis WHERE id=@id";
                        cmd.Parameters.AddWithValue("@id", txtIdJuego.Text);
                    }

                    cmd.CommandText = query;
                    cmd.Parameters.AddWithValue("@tit", txtTituloJuego.Text);
                    cmd.Parameters.AddWithValue("@desc", txtDescJuego.Text);
                    cmd.Parameters.AddWithValue("@pre", precio);
                    cmd.Parameters.AddWithValue("@gen", cmbGeneroJuego.Text);
                    cmd.Parameters.AddWithValue("@img", txtImagenJuego.Text);
                    cmd.Parameters.AddWithValue("@ban", txtBannerJuego.Text);
                    cmd.Parameters.AddWithValue("@vis", chkVisible.IsChecked);

                    cmd.ExecuteNonQuery();
                }
                BiTronixMsgBox.Show("Juego guardado correctamente.", "Éxito", BiTronixMsgBox.Type.Success);
                CargarJuegos(); LimpiarFormularioJuego();
            }
            catch (Exception ex) { BiTronixMsgBox.Show("Error al guardar juego: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
        }

        private void BtnEliminarJuego_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtIdJuego.Text)) return;
            if (BiTronixMsgBox.Show("¿Eliminar este juego?", "Confirmar", BiTronixMsgBox.Type.Confirmation, BiTronixMsgBox.Buttons.YesNo))
            {
                try
                {
                    using (var conn = db.GetConnection()) { new MySqlCommand("DELETE FROM videojuegos WHERE id=" + txtIdJuego.Text, conn).ExecuteNonQuery(); }
                    CargarJuegos(); LimpiarFormularioJuego();
                }
                catch { }
            }
        }

        private void BtnLimpiarJuego_Click(object sender, RoutedEventArgs e) => LimpiarFormularioJuego();
        private void BtnRefrescarJuegos_Click(object sender, RoutedEventArgs e) => CargarJuegos();
        private void LimpiarFormularioJuego() { txtIdJuego.Text = ""; txtTituloJuego.Text = ""; txtPrecioJuego.Text = ""; txtImagenJuego.Text = ""; txtBannerJuego.Text = ""; txtDescJuego.Text = ""; cmbGeneroJuego.SelectedIndex = -1; chkVisible.IsChecked = true; dgJuegos.SelectedItem = null; btnEliminarJuego.IsEnabled = false; }

        // ==========================================
        // GESTIÓN DE USUARIOS
        // ==========================================

        private void CargarUsuarios()
        {
            listaUsuarios.Clear();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "SELECT * FROM usuarios";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Usuario u = new Usuario
                            {
                                Id = reader.GetInt32("id"),
                                Nombre = reader.GetString("nombre_usuario"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                                Rol = reader.GetString("rol"),
                                EstadoCuenta = reader.IsDBNull(reader.GetOrdinal("estado_cuenta")) ? "activo" : reader.GetString("estado_cuenta"),
                                MotivoBaneo = reader.IsDBNull(reader.GetOrdinal("motivo_baneo")) ? "" : reader.GetString("motivo_baneo"),
                                TipoAdmin = reader.IsDBNull(reader.GetOrdinal("tipo_admin")) ? null : reader.GetString("tipo_admin")
                            };
                            if (!reader.IsDBNull(reader.GetOrdinal("fin_baneo"))) u.FinBaneo = reader.GetDateTime("fin_baneo");
                            listaUsuarios.Add(u);
                        }
                    }
                }
                dgUsuarios.ItemsSource = null; dgUsuarios.ItemsSource = listaUsuarios;
            }
            catch { }
        }

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Usuario u = (Usuario)dgUsuarios.SelectedItem;
            if (u != null)
            {
                txtId.Text = u.Id.ToString(); txtNombre.Text = u.Nombre; txtEmail.Text = u.Email; txtPassword.Password = "";
                cmbEstado.Text = u.EstadoCuenta; txtMotivo.Text = u.MotivoBaneo; cmbDuracionBaneo.SelectedIndex = 0;
                usuarioOriginalSnapshot = new Usuario { Id = u.Id, Nombre = u.Nombre, Email = u.Email, Rol = u.Rol, EstadoCuenta = u.EstadoCuenta, MotivoBaneo = u.MotivoBaneo };

                ConfigurarPermisosRol(u);
                cmbRol.Text = u.Rol;

                // Cargar Tipo Admin si existe y es admin
                if (u.Rol == "admin" && u.TipoAdmin != null)
                {
                    foreach (ComboBoxItem item in cmbTipoAdmin.Items)
                        if (item.Content.ToString() == u.TipoAdmin) cmbTipoAdmin.SelectedItem = item;
                }
                else cmbTipoAdmin.SelectedIndex = -1;

                if (u.Id == 1) btnEliminar.IsEnabled = false; else btnEliminar.IsEnabled = true;
                if (u.Id == adminLogueado.Id) { cmbEstado.IsEnabled = false; cmbDuracionBaneo.IsEnabled = false; } else { cmbEstado.IsEnabled = true; cmbDuracionBaneo.IsEnabled = true; }
                if (adminLogueado.Id != 1 && u.Id == 1) { btnGuardar.IsEnabled = false; btnEliminar.IsEnabled = false; cmbEstado.IsEnabled = false; } else { btnGuardar.IsEnabled = true; }
            }
        }

        private void CmbRol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRol.SelectedItem == null) return;
            string rolSeleccionado = (cmbRol.SelectedItem as ComboBoxItem).Content.ToString();

            // Solo mostramos el panel si soy SuperAdmin y el rol es Admin
            if (adminLogueado.Id == 1 && rolSeleccionado == "admin")
            {
                PanelTipoAdmin.Visibility = Visibility.Visible;
            }
            else
            {
                PanelTipoAdmin.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfigurarPermisosRol(Usuario usuarioSeleccionado)
        {
            cmbRol.Items.Clear();
            cmbRol.Items.Add(new ComboBoxItem { Content = "usuario" });

            if (adminLogueado.Id == 1)
            {
                cmbRol.Items.Add(new ComboBoxItem { Content = "admin" });
                cmbRol.IsEnabled = true;
                if (usuarioSeleccionado != null && usuarioSeleccionado.Id == 1) cmbRol.IsEnabled = false;
            }
            else
            {
                if (usuarioSeleccionado != null)
                {
                    if (usuarioSeleccionado.Rol == "admin") { cmbRol.Items.Add(new ComboBoxItem { Content = "admin" }); cmbRol.Text = "admin"; cmbRol.IsEnabled = false; }
                    else cmbRol.IsEnabled = true;
                }
                else cmbRol.IsEnabled = true;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text; string email = txtEmail.Text; string rol = cmbRol.Text; string estado = cmbEstado.Text; string motivo = txtMotivo.Text;
            string tipoAdmin = null;

            if (rol == "admin" && PanelTipoAdmin.Visibility == Visibility.Visible && cmbTipoAdmin.SelectedItem != null)
            {
                tipoAdmin = (cmbTipoAdmin.SelectedItem as ComboBoxItem).Content.ToString();
            }

            DateTime? finBaneo = null;
            if (cmbDuracionBaneo.SelectedIndex > 0)
            {
                string duracion = cmbDuracionBaneo.Text;
                if (duracion.Contains("1 Hora")) finBaneo = DateTime.Now.AddHours(1);
                else if (duracion.Contains("24 Horas")) finBaneo = DateTime.Now.AddHours(24);
                else if (duracion.Contains("1 Semana")) finBaneo = DateTime.Now.AddDays(7);
                else if (duracion.Contains("Indefinido")) finBaneo = DateTime.Now.AddYears(50);
            }

            try
            {
                using (var conn = db.GetConnection())
                {
                    if (string.IsNullOrEmpty(txtId.Text)) // INSERT
                    {
                        string passwordHash = string.IsNullOrEmpty(txtPassword.Password) ? "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4" : GenerarHash(txtPassword.Password);
                        string query = "INSERT INTO usuarios (nombre_usuario, email, rol, tipo_admin, estado_cuenta, motivo_baneo, fin_baneo, password_hash, fecha_registro) VALUES (@nom, @mail, @rol, @tipo, @est, @mot, @fin, @pass, NOW())";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", nombre); cmd.Parameters.AddWithValue("@mail", email); cmd.Parameters.AddWithValue("@rol", rol);
                        cmd.Parameters.AddWithValue("@tipo", tipoAdmin);
                        cmd.Parameters.AddWithValue("@est", estado); cmd.Parameters.AddWithValue("@mot", motivo); cmd.Parameters.AddWithValue("@fin", finBaneo);
                        cmd.Parameters.AddWithValue("@pass", passwordHash);
                        cmd.ExecuteNonQuery();
                    }
                    else // UPDATE
                    {
                        string query = "UPDATE usuarios SET nombre_usuario=@nom, email=@mail, rol=@rol, tipo_admin=@tipo, estado_cuenta=@est, motivo_baneo=@mot";
                        if (finBaneo != null) query += ", fin_baneo=@fin"; else if (estado == "activo") query += ", fin_baneo=NULL";
                        query += " WHERE id=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", txtId.Text); cmd.Parameters.AddWithValue("@nom", nombre); cmd.Parameters.AddWithValue("@mail", email); cmd.Parameters.AddWithValue("@rol", rol);
                        cmd.Parameters.AddWithValue("@tipo", tipoAdmin);
                        cmd.Parameters.AddWithValue("@est", estado); cmd.Parameters.AddWithValue("@mot", motivo); if (finBaneo != null) cmd.Parameters.AddWithValue("@fin", finBaneo);
                        cmd.ExecuteNonQuery();

                        if (!string.IsNullOrEmpty(txtPassword.Password))
                        {
                            string nuevoHash = GenerarHash(txtPassword.Password);
                            new MySqlCommand($"UPDATE usuarios SET password_hash = '{nuevoHash}' WHERE id = {txtId.Text}", conn).ExecuteNonQuery();
                        }
                    }
                }
                BiTronixMsgBox.Show("Usuario guardado correctamente.", "Éxito", BiTronixMsgBox.Type.Success);
                CargarUsuarios(); LimpiarFormulario();
            }
            catch (Exception ex) { BiTronixMsgBox.Show("Error al guardar: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e) { if (string.IsNullOrEmpty(txtId.Text)) return; if (BiTronixMsgBox.Show("¿Eliminar usuario?", "Confirmar", BiTronixMsgBox.Type.Confirmation, BiTronixMsgBox.Buttons.YesNo)) { try { using (var conn = db.GetConnection()) new MySqlCommand("DELETE FROM usuarios WHERE id=" + txtId.Text, conn).ExecuteNonQuery(); CargarUsuarios(); LimpiarFormulario(); } catch { } } }
        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();
        private void LimpiarFormulario() { txtId.Text = ""; txtNombre.Text = ""; txtEmail.Text = ""; txtPassword.Password = ""; txtMotivo.Text = ""; cmbRol.SelectedIndex = -1; cmbEstado.SelectedIndex = -1; cmbDuracionBaneo.SelectedIndex = 0; PanelTipoAdmin.Visibility = Visibility.Collapsed; dgUsuarios.SelectedItem = null; usuarioOriginalSnapshot = null; ConfigurarPermisosRol(null); cmbEstado.IsEnabled = true; cmbDuracionBaneo.IsEnabled = true; btnGuardar.IsEnabled = true; }
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) dgUsuarios.ItemsSource = listaUsuarios; else dgUsuarios.ItemsSource = listaUsuarios.FindAll(u => u.Nombre.ToLower().Contains(txtBuscar.Text.ToLower())); }
        private void BtnRefrescar_Click(object sender, RoutedEventArgs e) { CargarUsuarios(); txtBuscar.Text = ""; }
        private void CmbEstado_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (cmbEstado.SelectedItem == null || cmbDuracionBaneo == null) return; string est = (cmbEstado.SelectedItem as ComboBoxItem).Content.ToString(); if ((est == "baneado" || est == "suspendido") && cmbDuracionBaneo.SelectedIndex == 0) cmbDuracionBaneo.SelectedIndex = 4; else if (est == "activo") cmbDuracionBaneo.SelectedIndex = 0; }
        private void CmbDuracionBaneo_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (cmbDuracionBaneo.SelectedItem == null || cmbEstado == null) return; if (cmbDuracionBaneo.SelectedIndex > 0) foreach (ComboBoxItem item in cmbEstado.Items) if (item.Content.ToString() == "baneado") { cmbEstado.SelectedItem = item; break; } }

        private void CargarLogs() { var logs = new List<LogEntry>(); try { using (var conn = db.GetConnection()) { MySqlCommand cmd = new MySqlCommand("SELECT * FROM audit_logs ORDER BY fecha DESC", conn); using (MySqlDataReader reader = cmd.ExecuteReader()) { while (reader.Read()) { logs.Add(new LogEntry { Id = reader.GetInt32("id"), UsuarioResponsable = reader.GetString("usuario_responsable"), Accion = reader.GetString("accion"), Detalles = reader.GetString("detalles"), Fecha = reader.GetDateTime("fecha").ToString("yyyy-MM-dd HH:mm:ss") }); } } } dgAuditoria.ItemsSource = logs; } catch { } }
        private void BtnRefrescarLogs_Click(object sender, RoutedEventArgs e) => CargarLogs();
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e) { try { string ruta = "Reporte_Logs.csv"; using (StreamWriter sw = new StreamWriter(ruta)) { sw.WriteLine("ID;FECHA;RESPONSABLE;ACCION;DETALLES"); foreach (LogEntry log in dgAuditoria.Items) sw.WriteLine($"{log.Id};{log.Fecha};{log.UsuarioResponsable};{log.Accion};{log.Detalles}"); } BiTronixMsgBox.Show($"CSV generado: {Path.GetFullPath(ruta)}", "Exportación", BiTronixMsgBox.Type.Success); } catch { } }
        private void BtnExportPDF_Click(object sender, RoutedEventArgs e) { try { string ruta = "Reporte_BiTronix.txt"; using (StreamWriter sw = new StreamWriter(ruta)) { sw.WriteLine("LOGS"); foreach (LogEntry log in dgAuditoria.Items) sw.WriteLine($"[{log.Fecha}] {log.UsuarioResponsable}: {log.Accion} > {log.Detalles}"); } BiTronixMsgBox.Show($"Informe generado: {Path.GetFullPath(ruta)}", "Exportación", BiTronixMsgBox.Type.Success); System.Diagnostics.Process.Start("notepad.exe", ruta); } catch { } }

        private void CargarApelaciones() { var lista = new List<ApelacionEntry>(); try { using (var conn = db.GetConnection()) { string query = "SELECT a.id, a.usuario_afectado_id, u.nombre_usuario, a.mensaje_apelacion, a.estado, a.fecha_solicitud FROM apelaciones a JOIN usuarios u ON a.usuario_afectado_id = u.id WHERE a.estado = 'pendiente' ORDER BY a.fecha_solicitud ASC"; MySqlCommand cmd = new MySqlCommand(query, conn); using (MySqlDataReader reader = cmd.ExecuteReader()) { while (reader.Read()) { lista.Add(new ApelacionEntry { Id = reader.GetInt32("id"), UsuarioId = reader.GetInt32("usuario_afectado_id"), NombreUsuario = reader.GetString("nombre_usuario"), Mensaje = reader.GetString("mensaje_apelacion"), Estado = reader.GetString("estado"), Fecha = reader.GetDateTime("fecha_solicitud").ToString("yyyy-MM-dd") }); } } } dgApelaciones.ItemsSource = lista; } catch { } }
        private void DgApelaciones_SelectionChanged(object sender, SelectionChangedEventArgs e) { ApelacionEntry item = (ApelacionEntry)dgApelaciones.SelectedItem; if (item != null) { txtMensajeApelacion.Text = item.Mensaje; btnAceptarApelacion.IsEnabled = true; btnRechazarApelacion.IsEnabled = true; } else { txtMensajeApelacion.Text = "..."; btnAceptarApelacion.IsEnabled = false; btnRechazarApelacion.IsEnabled = false; } }
        private void BtnAceptarApelacion_Click(object sender, RoutedEventArgs e) => ProcesarApelacion(true);
        private void BtnRechazarApelacion_Click(object sender, RoutedEventArgs e) => ProcesarApelacion(false);
        private void ProcesarApelacion(bool aceptada) { ApelacionEntry item = (ApelacionEntry)dgApelaciones.SelectedItem; if (item == null) return; try { using (var conn = db.GetConnection()) { string est = aceptada ? "aceptada" : "rechazada"; new MySqlCommand($"UPDATE apelaciones SET estado = '{est}' WHERE id = {item.Id}", conn).ExecuteNonQuery(); if (aceptada) new MySqlCommand($"UPDATE usuarios SET estado_cuenta = 'activo', fin_baneo = NULL WHERE id = {item.UsuarioId}", conn).ExecuteNonQuery(); } BiTronixMsgBox.Show("Apelación procesada.", "Info", BiTronixMsgBox.Type.Info); CargarApelaciones(); CargarUsuarios(); VerificarNotificaciones(); } catch { } }
        private void VerificarNotificaciones() { try { using (var conn = db.GetConnection()) { long count = (long)new MySqlCommand("SELECT COUNT(*) FROM apelaciones WHERE estado = 'pendiente'", conn).ExecuteScalar(); if (count > 0) { badgeNotificacion.Visibility = Visibility.Visible; txtNumApelaciones.Text = count.ToString(); BiTronixMsgBox.Show($"Tienes {count} apelaciones pendientes.", "Atención", BiTronixMsgBox.Type.Warning); } else badgeNotificacion.Visibility = Visibility.Collapsed; } } catch { } }

        public class LogEntry { public int Id { get; set; } public string UsuarioResponsable { get; set; } public string Accion { get; set; } public string Detalles { get; set; } public string Fecha { get; set; } }
        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove(); }
        private string GenerarHash(string textoPlano) { using (SHA256 sha256 = SHA256.Create()) { byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano)); StringBuilder builder = new StringBuilder(); for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2")); return builder.ToString(); } }
    }
}