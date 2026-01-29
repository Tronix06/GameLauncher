using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

namespace GameLauncher
{
    public class GestorUsuarios
    {
        private DatabaseConnection dbConnection;

        public GestorUsuarios()
        {
            dbConnection = new DatabaseConnection();
        }

        // --- LOGIN INTELIGENTE (SaaS) ---
        // (Este método se mantiene igual que tu original, solo lo comprimo visualmente aquí)
        public string ValidarLogin(string usuario, string password)
        {
            MySqlConnection conn = null;
            Usuario datosUsuario = null;

            try
            {
                conn = dbConnection.GetConnection();
                // OBTENER ESTADO DEL USUARIO
                string queryInfo = "SELECT id, password_hash, estado_cuenta, fin_baneo, intentos_fallidos FROM usuarios WHERE nombre_usuario = @user";
                MySqlCommand cmdInfo = new MySqlCommand(queryInfo, conn);
                cmdInfo.Parameters.AddWithValue("@user", usuario);

                using (MySqlDataReader reader = cmdInfo.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        datosUsuario = new Usuario
                        {
                            Id = reader.GetInt32("id"),
                            PasswordHash = reader.GetString("password_hash"),
                            EstadoCuenta = reader.IsDBNull(reader.GetOrdinal("estado_cuenta")) ? "activo" : reader.GetString("estado_cuenta"),
                            FinBaneo = reader.IsDBNull(reader.GetOrdinal("fin_baneo")) ? (DateTime?)null : reader.GetDateTime("fin_baneo"),
                            IntentosFallidos = reader.GetInt32("intentos_fallidos")
                        };
                    }
                }

                if (datosUsuario == null) return "NO_EXISTE";
                if (datosUsuario.EstadoCuenta == "suspendido") { LogManager.RegistrarAccion(usuario, "LOGIN_DENEGADO", "Cuenta suspendida"); return "CUENTA_SUSPENDIDA"; }

                if (datosUsuario.EstadoCuenta == "baneado")
                {
                    if (datosUsuario.FinBaneo.HasValue && datosUsuario.FinBaneo.Value > DateTime.Now) return "CUENTA_BANEADA_TEMP";
                    else { LevantarCastigo(datosUsuario.Id); LogManager.RegistrarAccion("SISTEMA", "AUTO_UNBAN", "Castigo expirado"); }
                }

                string passHashIngresada = GenerarHash(password);
                if (datosUsuario.PasswordHash == passHashIngresada)
                {
                    ResetearIntentos(datosUsuario.Id);
                    LogManager.RegistrarAccion(usuario, "LOGIN_EXITO", "Login correcto");
                    return "OK";
                }
                else
                {
                    AumentarIntentos(datosUsuario.Id);
                    LogManager.RegistrarAccion(usuario, "LOGIN_FAIL", "Pass incorrecta");
                    return "PASS_INCORRECTA";
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("ERROR: " + ex.Message); return "ERROR_CONEXION"; }
            finally { dbConnection.CloseConnection(); }
        }

        // --- OBTENER DATOS COMPLETOS (ACTUALIZADO P5) ---
        public Usuario ObtenerDatosUsuario(string nombreUsuario)
        {
            Usuario usuario = null;
            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "SELECT * FROM usuarios WHERE nombre_usuario = @user";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", nombreUsuario);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new Usuario
                            {
                                Id = reader.GetInt32("id"),
                                Nombre = reader.GetString("nombre_usuario"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email"),
                                Rol = reader.IsDBNull(reader.GetOrdinal("rol")) ? "usuario" : reader.GetString("rol"),
                                EstadoCuenta = reader.IsDBNull(reader.GetOrdinal("estado_cuenta")) ? "activo" : reader.GetString("estado_cuenta"),
                                MotivoBaneo = reader.IsDBNull(reader.GetOrdinal("motivo_baneo")) ? "" : reader.GetString("motivo_baneo"),
                                FinBaneo = reader.IsDBNull(reader.GetOrdinal("fin_baneo")) ? (DateTime?)null : reader.GetDateTime("fin_baneo"),

                                // --- NUEVO P5: LEER TIPO ADMIN ---
                                TipoAdmin = reader.IsDBNull(reader.GetOrdinal("tipo_admin")) ? null : reader.GetString("tipo_admin")
                            };
                        }
                    }
                }
            }
            catch { return null; }
            return usuario;
        }

        // --- REGISTRO DE NUEVOS USUARIOS ---
        public bool RegistrarUsuario(string usuario, string password, string email)
        {
            if (VerificarSiUsuarioExiste(usuario)) return false;
            string passHash = GenerarHash(password);

            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "INSERT INTO usuarios (nombre_usuario, password_hash, email, rol, estado_cuenta, fecha_registro) VALUES (@user, @pass, @email, 'usuario', 'activo', NOW())";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", usuario);
                    cmd.Parameters.AddWithValue("@pass", passHash);
                    cmd.Parameters.AddWithValue("@email", email);

                    int filas = cmd.ExecuteNonQuery();
                    if (filas > 0)
                    {
                        LogManager.RegistrarAccion(usuario, "REGISTRO", "Nuevo usuario creado");
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // --- MÉTODOS AUXILIARES (Sin cambios) ---
        private void LevantarCastigo(int idUsuario)
        {
            try { using (MySqlConnection conn = dbConnection.GetConnection()) { new MySqlCommand("UPDATE usuarios SET estado_cuenta = 'activo', fin_baneo = NULL, motivo_baneo = NULL WHERE id = " + idUsuario, conn).ExecuteNonQuery(); } } catch { }
        }
        private void AumentarIntentos(int idUsuario)
        {
            try { using (MySqlConnection conn = dbConnection.GetConnection()) { new MySqlCommand("UPDATE usuarios SET intentos_fallidos = intentos_fallidos + 1 WHERE id = " + idUsuario, conn).ExecuteNonQuery(); } } catch { }
        }
        private void ResetearIntentos(int idUsuario)
        {
            try { using (MySqlConnection conn = dbConnection.GetConnection()) { new MySqlCommand("UPDATE usuarios SET intentos_fallidos = 0 WHERE id = " + idUsuario, conn).ExecuteNonQuery(); } } catch { }
        }
        private bool VerificarSiUsuarioExiste(string usuario)
        {
            try { using (MySqlConnection conn = dbConnection.GetConnection()) { MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM usuarios WHERE nombre_usuario = @u", conn); cmd.Parameters.AddWithValue("@u", usuario); return (long)cmd.ExecuteScalar() > 0; } } catch { return false; }
        }
        private string GenerarHash(string textoPlano)
        {
            using (SHA256 sha256 = SHA256.Create()) { byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(textoPlano)); StringBuilder builder = new StringBuilder(); for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2")); return builder.ToString(); }
        }
    }
}