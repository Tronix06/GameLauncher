using System;
using MySql.Data.MySqlClient;

namespace GameLauncher
{
    public static class LogManager
    {
        // Método estático para registrar eventos sin instanciar la clase
        public static void RegistrarAccion(string usuarioResponsable, string accion, string detalles)
        {
            try
            {
                DatabaseConnection db = new DatabaseConnection();
                using (MySqlConnection conn = db.GetConnection())
                {
                    string query = "INSERT INTO audit_logs (usuario_responsable, accion, detalles, fecha) VALUES (@user, @accion, @detalles, NOW())";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", usuarioResponsable);
                    cmd.Parameters.AddWithValue("@accion", accion);
                    cmd.Parameters.AddWithValue("@detalles", detalles);

                    cmd.ExecuteNonQuery();
                }
                // No cerramos explícitamente porque el 'using' lo hace
            }
            catch
            {
                // Si falla el log, no detenemos el programa, pero sería ideal guardarlo en un txt de emergencia
            }
        }
    }
}