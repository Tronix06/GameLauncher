using System;
using System.Collections.Generic;
using System.Security.Cryptography; // Para el hash SHA256
using System.Text;
using MySql.Data.MySqlClient; // ¡Importante! El conector de MySQL

namespace GameLauncher
{
    public class GestorUsuarios
    {
        private DatabaseConnection dbConnection;

        public GestorUsuarios()
        {
            dbConnection = new DatabaseConnection();
        }

        // --- MÉTODO DE LOGIN (Requisito: Consulta SQL SELECT) ---
        public string ValidarLogin(string usuario, string password)
        {
            // 1. Convertimos la contraseña que escribe el usuario a Hash para compararla
            string passHash = GenerarHash(password);

            MySqlConnection conn = null;
            try
            {
                conn = dbConnection.GetConnection();

                // QUERY SQL: Buscamos si existe ese usuario Y esa contraseña
                string query = "SELECT COUNT(*) FROM usuarios WHERE nombre_usuario = @user AND password_hash = @pass";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                // Usamos parámetros (@user) para evitar Hackeos (SQL Injection) - ¡Vital para nota!
                cmd.Parameters.AddWithValue("@user", usuario);
                cmd.Parameters.AddWithValue("@pass", passHash);

                // ExecuteScalar devuelve el primer dato (el conteo de usuarios encontrados)
                long count = (long)cmd.ExecuteScalar();

                if (count > 0)
                {
                    return "OK"; // Existe y la contraseña coincide
                }
                else
                {
                    // Si no coincide, verificamos si es que el usuario no existe o la pass está mal
                    // (Opcional: Para simplificar la práctica, si no encuentra, devolvemos error genérico o PASS_INCORRECTA)
                    return VerificarSiUsuarioExiste(usuario) ? "PASS_INCORRECTA" : "NO_EXISTE";
                }
            }
            catch (Exception ex)
            {
                // Si falla la BBDD (está apagada, error de red...)
                System.Windows.MessageBox.Show("Error de BBDD: " + ex.Message);
                return "ERROR_CONEXION";
            }
            finally
            {
                dbConnection.CloseConnection();
            }
        }

        // Método auxiliar para saber si el error es de contraseña o de usuario
        private bool VerificarSiUsuarioExiste(string usuario)
        {
            try
            {
                MySqlConnection conn = dbConnection.GetConnection();
                string query = "SELECT COUNT(*) FROM usuarios WHERE nombre_usuario = @user";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", usuario);
                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
            catch { return false; }
        }

        // --- MÉTODO DE REGISTRO (Requisito: Sentencia INSERT) ---
        public bool RegistrarUsuario(string usuario, string password, string email)
        {
            // Primero verificamos si ya existe para no duplicar
            if (VerificarSiUsuarioExiste(usuario)) return false;

            string passHash = GenerarHash(password); // Encriptamos antes de guardar

            try
            {
                MySqlConnection conn = dbConnection.GetConnection();

                // QUERY SQL: Insertamos el nuevo usuario
                string query = "INSERT INTO usuarios (nombre_usuario, password_hash, email, fecha_registro) VALUES (@user, @pass, @email, NOW())";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@user", usuario);
                cmd.Parameters.AddWithValue("@pass", passHash);
                cmd.Parameters.AddWithValue("@email", email);

                int filasAfectadas = cmd.ExecuteNonQuery(); // Ejecuta el INSERT

                return filasAfectadas > 0; // Si guardó 1 fila, es true
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                dbConnection.CloseConnection();
            }
        }

        // --- UTILIDAD DE ENCRIPTACIÓN (Igual que antes) ---
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