using System;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace GameLauncher
{
    public partial class CastigoWindow : Window
    {
        private Usuario usuarioCastigado;

        // Constructor que recibe los datos del usuario baneado
        public CastigoWindow(Usuario usuario)
        {
            InitializeComponent();
            this.usuarioCastigado = usuario;
            CargarDatos();
        }

        private void CargarDatos()
        {
            txtMotivo.Text = string.IsNullOrEmpty(usuarioCastigado.MotivoBaneo) ? "Sin motivo especificado" : usuarioCastigado.MotivoBaneo;

            if (usuarioCastigado.EstadoCuenta == "suspendido")
            {
                txtTiempo.Text = "INDEFINIDO (Cuenta Suspendida)";
                txtTiempo.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                txtTiempo.Text = usuarioCastigado.ObtenerTiempoRestanteBaneo();
            }
        }

        private void BtnEnviarApelacion_Click(object sender, RoutedEventArgs e)
        {
            string mensaje = txtApelacion.Text;

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                // MENSAJE PERSONALIZADO (AVISO)
                BiTronixMsgBox.Show("Por favor, escribe un motivo para tu apelación.", "Campo Vacío", BiTronixMsgBox.Type.Warning);
                return;
            }

            try
            {
                DatabaseConnection db = new DatabaseConnection();
                using (var conn = db.GetConnection())
                {
                    // Insertamos la apelación en la BBDD
                    string query = "INSERT INTO apelaciones (usuario_afectado_id, mensaje_apelacion, estado, fecha_solicitud) VALUES (@uid, @msg, 'pendiente', NOW())";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@uid", usuarioCastigado.Id);
                    cmd.Parameters.AddWithValue("@msg", mensaje);

                    cmd.ExecuteNonQuery();

                    // Log de auditoría
                    LogManager.RegistrarAccion(usuarioCastigado.Nombre, "SOLICITUD_APELACION", "El usuario ha solicitado revisión de su baneo.");
                }

                // MENSAJE PERSONALIZADO (ÉXITO)
                BiTronixMsgBox.Show(
                    "Tu apelación ha sido enviada a los administradores.\nRevisaremos tu caso pronto.",
                    "Enviado Correctamente",
                    BiTronixMsgBox.Type.Success
                );

                // Deshabilitamos el botón para que no spamee
                btnEnviar.IsEnabled = false;
                btnEnviar.Content = "ENVIADO";
            }
            catch (Exception ex)
            {
                // MENSAJE PERSONALIZADO (ERROR)
                BiTronixMsgBox.Show("Error al enviar apelación: " + ex.Message, "Error Crítico", BiTronixMsgBox.Type.Error);
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            // EN LUGAR DE CERRAR LA APP, VOLVEMOS AL LOGIN
            MainWindow login = new MainWindow();
            login.Show();

            this.Close(); // Cerramos solo la ventana de castigo
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }
    }
}