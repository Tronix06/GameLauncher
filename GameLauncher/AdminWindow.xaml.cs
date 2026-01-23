using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using System.Security.Cryptography; // Para encriptar pass
using System.Text;

namespace GameLauncher
{
    public partial class AdminWindow : Window
    {
        private List<Usuario> listaUsuarios = new List<Usuario>();
        private DatabaseConnection db = new DatabaseConnection();
        private Usuario usuarioOriginalSnapshot;

        // Clase auxiliar para la lista de apelaciones
        public class ApelacionEntry
        {
            public int Id { get; set; }
            public int UsuarioId { get; set; }
            public string NombreUsuario { get; set; }
            public string Mensaje { get; set; }
            public string Estado { get; set; }
            public string Fecha { get; set; }
        }

        public AdminWindow()
        {
            InitializeComponent();
            CargarUsuarios();
            CargarLogs();
            CargarApelaciones();
            VerificarNotificaciones();

            txtAdminUser.Text = " | Sesión iniciada";
        }

        // --- SISTEMA DE NOTIFICACIONES ---
        private void VerificarNotificaciones()
        {
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "SELECT COUNT(*) FROM apelaciones WHERE estado = 'pendiente'";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    long count = (long)cmd.ExecuteScalar();

                    if (count > 0)
                    {
                        // 1. Mostrar Popup Personalizado (Amarillo/Warning)
                        BiTronixMsgBox.Show(
                            $"Tienes {count} apelaciones pendientes de revisión.\nPor favor, ve a la pestaña de Apelaciones.",
                            "Atención Admin",
                            BiTronixMsgBox.Type.Warning);

                        // 2. Encender el Badge Rojo en la pestaña
                        badgeNotificacion.Visibility = Visibility.Visible;
                        txtNumApelaciones.Text = count.ToString();
                    }
                    else
                    {
                        badgeNotificacion.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch { }
        }

        // --- GESTIÓN DE APELACIONES ---
        private void CargarApelaciones()
        {
            var lista = new List<ApelacionEntry>();
            try
            {
                using (var conn = db.GetConnection())
                {
                    string query = "SELECT a.id, a.usuario_afectado_id, u.nombre_usuario, a.mensaje_apelacion, a.estado, a.fecha_solicitud " +
                                   "FROM apelaciones a " +
                                   "JOIN usuarios u ON a.usuario_afectado_id = u.id " +
                                   "WHERE a.estado = 'pendiente' ORDER BY a.fecha_solicitud ASC";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ApelacionEntry
                            {
                                Id = reader.GetInt32("id"),
                                UsuarioId = reader.GetInt32("usuario_afectado_id"),
                                NombreUsuario = reader.GetString("nombre_usuario"),
                                Mensaje = reader.GetString("mensaje_apelacion"),
                                Estado = reader.GetString("estado"),
                                Fecha = reader.GetDateTime("fecha_solicitud").ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
                dgApelaciones.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                BiTronixMsgBox.Show("Error cargando apelaciones: " + ex.Message, "Error BBDD", BiTronixMsgBox.Type.Error);
            }
        }

        private void DgApelaciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApelacionEntry item = (ApelacionEntry)dgApelaciones.SelectedItem;
            if (item != null)
            {
                txtMensajeApelacion.Text = item.Mensaje;
                btnAceptarApelacion.IsEnabled = true;
                btnRechazarApelacion.IsEnabled = true;
            }
            else
            {
                txtMensajeApelacion.Text = "Selecciona una apelación...";
                btnAceptarApelacion.IsEnabled = false;
                btnRechazarApelacion.IsEnabled = false;
            }
        }

        private void BtnAceptarApelacion_Click(object sender, RoutedEventArgs e) => ProcesarApelacion(true);
        private void BtnRechazarApelacion_Click(object sender, RoutedEventArgs e) => ProcesarApelacion(false);

        private void ProcesarApelacion(bool aceptada)
        {
            ApelacionEntry item = (ApelacionEntry)dgApelaciones.SelectedItem;
            if (item == null) return;

            try
            {
                using (var conn = db.GetConnection())
                {
                    string nuevoEstado = aceptada ? "aceptada" : "rechazada";
                    string queryApelacion = "UPDATE apelaciones SET estado = @estado WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(queryApelacion, conn);
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado);
                    cmd.Parameters.AddWithValue("@id", item.Id);
                    cmd.ExecuteNonQuery();

                    if (aceptada)
                    {
                        string queryDesban = "UPDATE usuarios SET estado_cuenta = 'activo', fin_baneo = NULL, motivo_baneo = NULL WHERE id = @uid";
                        MySqlCommand cmd2 = new MySqlCommand(queryDesban, conn);
                        cmd2.Parameters.AddWithValue("@uid", item.UsuarioId);
                        cmd2.ExecuteNonQuery();
                    }
                }

                string accion = aceptada ? "APELACION_ACEPTADA" : "APELACION_RECHAZADA";
                LogManager.RegistrarAccion("Admin", accion, $"Usuario: {item.NombreUsuario}. Decisión: {accion}");

                // MENSAJE PERSONALIZADO SEGÚN DECISIÓN
                if (aceptada)
                    BiTronixMsgBox.Show("El usuario ha sido perdonado y desbaneado correctamente.", "Apelación Aceptada", BiTronixMsgBox.Type.Success);
                else
                    BiTronixMsgBox.Show("La apelación ha sido rechazada. El castigo se mantiene.", "Apelación Rechazada", BiTronixMsgBox.Type.Error);

                CargarApelaciones(); CargarUsuarios(); VerificarNotificaciones();
                txtMensajeApelacion.Text = ""; btnAceptarApelacion.IsEnabled = false; btnRechazarApelacion.IsEnabled = false;
            }
            catch (Exception ex)
            {
                BiTronixMsgBox.Show("Error procesando: " + ex.Message, "Error", BiTronixMsgBox.Type.Error);
            }
        }

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
                cmbRol.Text = u.Rol; cmbEstado.Text = u.EstadoCuenta; txtMotivo.Text = u.MotivoBaneo; cmbDuracionBaneo.SelectedIndex = 0;
                usuarioOriginalSnapshot = new Usuario { Id = u.Id, Nombre = u.Nombre, Email = u.Email, Rol = u.Rol, EstadoCuenta = u.EstadoCuenta, MotivoBaneo = u.MotivoBaneo };
                if (u.Id == 1) btnEliminar.IsEnabled = false; else btnEliminar.IsEnabled = true;
            }
        }

        private void CmbEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEstado.SelectedItem == null || cmbDuracionBaneo == null) return;
            string nuevoEstado = (cmbEstado.SelectedItem as ComboBoxItem).Content.ToString();
            if ((nuevoEstado == "baneado" || nuevoEstado == "suspendido") && cmbDuracionBaneo.SelectedIndex == 0) cmbDuracionBaneo.SelectedIndex = 4;
            else if (nuevoEstado == "activo") cmbDuracionBaneo.SelectedIndex = 0;
        }

        private void CmbDuracionBaneo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDuracionBaneo.SelectedItem == null || cmbEstado == null) return;
            if (cmbDuracionBaneo.SelectedIndex > 0) foreach (ComboBoxItem item in cmbEstado.Items) if (item.Content.ToString() == "baneado") { cmbEstado.SelectedItem = item; break; }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text; string email = txtEmail.Text; string rol = cmbRol.Text; string estado = cmbEstado.Text; string motivo = txtMotivo.Text;
            DateTime? finBaneo = null; string textoDuracionLog = "";

            if (cmbDuracionBaneo.SelectedIndex > 0)
            {
                string duracion = cmbDuracionBaneo.Text; textoDuracionLog = $" ({duracion})";
                if (duracion.Contains("1 Hora")) finBaneo = DateTime.Now.AddHours(1);
                else if (duracion.Contains("24 Horas")) finBaneo = DateTime.Now.AddHours(24);
                else if (duracion.Contains("1 Semana")) finBaneo = DateTime.Now.AddDays(7);
                else if (duracion.Contains("Indefinido")) finBaneo = DateTime.Now.AddYears(50);
            }

            try
            {
                using (var conn = db.GetConnection())
                {
                    // VALIDACIÓN DE DUPLICADOS CON MENSAJE BONITO
                    if (string.IsNullOrEmpty(txtId.Text))
                    {
                        MySqlCommand checkCmd = new MySqlCommand("SELECT COUNT(*) FROM usuarios WHERE nombre_usuario = @u", conn);
                        checkCmd.Parameters.AddWithValue("@u", nombre);
                        if ((long)checkCmd.ExecuteScalar() > 0)
                        {
                            BiTronixMsgBox.Show($"El usuario '{nombre}' ya existe.\nPor favor, elige otro nombre.", "Nombre Duplicado", BiTronixMsgBox.Type.Warning);
                            return;
                        }
                    }

                    if (string.IsNullOrEmpty(txtId.Text))
                    {
                        // INSERTAR
                        string passwordHash = string.IsNullOrEmpty(txtPassword.Password)
                            ? "03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4" // Default 1234
                            : GenerarHash(txtPassword.Password);

                        string query = "INSERT INTO usuarios (nombre_usuario, email, rol, estado_cuenta, motivo_baneo, fin_baneo, password_hash, fecha_registro) VALUES (@nom, @mail, @rol, @est, @mot, @fin, @pass, NOW())";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@nom", nombre); cmd.Parameters.AddWithValue("@mail", email); cmd.Parameters.AddWithValue("@rol", rol); cmd.Parameters.AddWithValue("@est", estado); cmd.Parameters.AddWithValue("@mot", motivo); cmd.Parameters.AddWithValue("@fin", finBaneo);
                        cmd.Parameters.AddWithValue("@pass", passwordHash);

                        cmd.ExecuteNonQuery();
                        LogManager.RegistrarAccion("Admin", "CREAR", $"Alta de usuario: {nombre} ({rol})");
                    }
                    else
                    {
                        // ACTUALIZAR
                        string query = "UPDATE usuarios SET nombre_usuario=@nom, email=@mail, rol=@rol, estado_cuenta=@est, motivo_baneo=@mot";
                        if (finBaneo != null) query += ", fin_baneo=@fin"; else if (estado == "activo") query += ", fin_baneo=NULL";
                        query += " WHERE id=@id";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", txtId.Text); cmd.Parameters.AddWithValue("@nom", nombre); cmd.Parameters.AddWithValue("@mail", email); cmd.Parameters.AddWithValue("@rol", rol); cmd.Parameters.AddWithValue("@est", estado); cmd.Parameters.AddWithValue("@mot", motivo); if (finBaneo != null) cmd.Parameters.AddWithValue("@fin", finBaneo);
                        cmd.ExecuteNonQuery();

                        // CAMBIO DE CONTRASEÑA ENCRIPTADA
                        List<string> cambios = new List<string>();
                        if (!string.IsNullOrEmpty(txtPassword.Password))
                        {
                            string nuevoHash = GenerarHash(txtPassword.Password);
                            string queryPass = "UPDATE usuarios SET password_hash = @ph WHERE id = @id";
                            MySqlCommand cmdPass = new MySqlCommand(queryPass, conn);
                            cmdPass.Parameters.AddWithValue("@ph", nuevoHash);
                            cmdPass.Parameters.AddWithValue("@id", txtId.Text);
                            cmdPass.ExecuteNonQuery();
                            cambios.Add("Contraseña RESETEADA");
                        }

                        if (usuarioOriginalSnapshot.Nombre != nombre) cambios.Add($"Nombre: {usuarioOriginalSnapshot.Nombre}->{nombre}");
                        if (usuarioOriginalSnapshot.Rol != rol) cambios.Add($"Rol: {usuarioOriginalSnapshot.Rol}->{rol}");
                        if (usuarioOriginalSnapshot.EstadoCuenta != estado) cambios.Add($"Estado: {usuarioOriginalSnapshot.EstadoCuenta}->{estado}{textoDuracionLog}");

                        string detallesLog = cambios.Count > 0 ? string.Join(", ", cambios) : "Actualización sin cambios críticos";
                        LogManager.RegistrarAccion("Admin", "MODIFICAR", $"Usuario ID {txtId.Text}: {detallesLog}");
                    }
                }

                // MENSAJE DE ÉXITO
                BiTronixMsgBox.Show("Los datos han sido guardados correctamente en la base de datos.", "Operación Exitosa", BiTronixMsgBox.Type.Success);

                CargarUsuarios(); LimpiarFormulario();
            }
            catch (Exception ex)
            {
                BiTronixMsgBox.Show("Error al guardar: " + ex.Message, "Error Crítico", BiTronixMsgBox.Type.Error);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtId.Text)) return;

            // MENSAJE DE CONFIRMACIÓN MORADO (SÍ/NO)
            if (BiTronixMsgBox.Show("¿Seguro que quieres eliminar a este usuario permanentemente?\nEsta acción no se puede deshacer.", "Confirmar Borrado", BiTronixMsgBox.Type.Confirmation, BiTronixMsgBox.Buttons.YesNo))
            {
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        new MySqlCommand("DELETE FROM usuarios WHERE id=" + txtId.Text, conn).ExecuteNonQuery();
                    }
                    LogManager.RegistrarAccion("Admin", "ELIMINAR", $"Borró usuario ID {txtId.Text}");
                    CargarUsuarios();
                    LimpiarFormulario();
                    BiTronixMsgBox.Show("Usuario eliminado.", "Info", BiTronixMsgBox.Type.Info);
                }
                catch (Exception ex)
                {
                    BiTronixMsgBox.Show("Error al eliminar: " + ex.Message, "Error", BiTronixMsgBox.Type.Error);
                }
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();
        private void LimpiarFormulario() { txtId.Text = ""; txtNombre.Text = ""; txtEmail.Text = ""; txtPassword.Password = ""; txtMotivo.Text = ""; cmbRol.SelectedIndex = -1; cmbEstado.SelectedIndex = -1; cmbDuracionBaneo.SelectedIndex = 0; dgUsuarios.SelectedItem = null; }
        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e) { if (string.IsNullOrWhiteSpace(txtBuscar.Text)) dgUsuarios.ItemsSource = listaUsuarios; else dgUsuarios.ItemsSource = listaUsuarios.FindAll(u => u.Nombre.ToLower().Contains(txtBuscar.Text.ToLower())); }
        private void BtnRefrescar_Click(object sender, RoutedEventArgs e) { CargarUsuarios(); txtBuscar.Text = ""; }

        public class LogEntry { public int Id { get; set; } public string UsuarioResponsable { get; set; } public string Accion { get; set; } public string Detalles { get; set; } public string Fecha { get; set; } }
        private void CargarLogs()
        {
            var logs = new List<LogEntry>();
            try { using (var conn = db.GetConnection()) { MySqlCommand cmd = new MySqlCommand("SELECT * FROM audit_logs ORDER BY fecha DESC", conn); using (MySqlDataReader reader = cmd.ExecuteReader()) { while (reader.Read()) { logs.Add(new LogEntry { Id = reader.GetInt32("id"), UsuarioResponsable = reader.GetString("usuario_responsable"), Accion = reader.GetString("accion"), Detalles = reader.GetString("detalles"), Fecha = reader.GetDateTime("fecha").ToString("yyyy-MM-dd HH:mm:ss") }); } } } dgAuditoria.ItemsSource = logs; } catch { }
        }
        private void BtnRefrescarLogs_Click(object sender, RoutedEventArgs e) => CargarLogs();

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ruta = "Reporte_Logs.csv";
                using (StreamWriter sw = new StreamWriter(ruta))
                {
                    sw.WriteLine("ID;FECHA;RESPONSABLE;ACCION;DETALLES");
                    foreach (LogEntry log in dgAuditoria.Items) sw.WriteLine($"{log.Id};{log.Fecha};{log.UsuarioResponsable};{log.Accion};{log.Detalles}");
                }
                BiTronixMsgBox.Show($"CSV generado en:\n{Path.GetFullPath(ruta)}", "Exportación Exitosa", BiTronixMsgBox.Type.Success);
            }
            catch (Exception ex) { BiTronixMsgBox.Show("Error: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
        }

        private void BtnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ruta = "Reporte_BiTronix.txt";
                using (StreamWriter sw = new StreamWriter(ruta))
                {
                    sw.WriteLine("=== BITRONIX - LOGS ===");
                    foreach (LogEntry log in dgAuditoria.Items)
                    {
                        sw.WriteLine($"[{log.Fecha}] {log.UsuarioResponsable}: {log.Accion}");
                        sw.WriteLine($"   > {log.Detalles}");
                        sw.WriteLine("---");
                    }
                }
                BiTronixMsgBox.Show($"Informe generado en:\n{Path.GetFullPath(ruta)}", "Exportación Exitosa", BiTronixMsgBox.Type.Success);
                System.Diagnostics.Process.Start("notepad.exe", ruta);
            }
            catch (Exception ex) { BiTronixMsgBox.Show("Error: " + ex.Message, "Error", BiTronixMsgBox.Type.Error); }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove(); }

        // --- ENCRIPTACIÓN PARA GUARDAR PASS ---
        private string GenerarHash(string textoPlano)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}