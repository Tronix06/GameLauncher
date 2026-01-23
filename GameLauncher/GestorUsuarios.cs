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
        public string ValidarLogin(string usuario, string password)
        {
            MySqlConnection conn = null;
            Usuario datosUsuario = null;

            try
            {
                conn = dbConnection.GetConnection();

                // 1. OBTENER ESTADO DEL USUARIO (Antes de comprobar contraseña)
                // Necesitamos saber si está baneado para ver si lo perdonamos automáticamente
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

                // CASO A: El usuario no existe
                if (datosUsuario == null) return "NO_EXISTE";

                // CASO B: Cuenta SUSPENDIDA (Baneo permanente)
                if (datosUsuario.EstadoCuenta == "suspendido")
                {
                    LogManager.RegistrarAccion(usuario, "LOGIN_DENEGADO", "Intento de acceso a cuenta suspendida");
                    return "CUENTA_SUSPENDIDA";
                }

                // CASO C: Cuenta BANEADA (Temporal) - Lógica de Tiempo Real
                if (datosUsuario.EstadoCuenta == "baneado")
                {
                    if (datosUsuario.FinBaneo.HasValue && datosUsuario.FinBaneo.Value > DateTime.Now)
                    {
                        // Aún le queda tiempo de castigo
                        LogManager.RegistrarAccion(usuario, "LOGIN_DENEGADO", "Usuario baneado temporalmente intentó entrar");
                        return "CUENTA_BANEADA_TEMP";
                    }
                    else
                    {
                        // ¡EL CASTIGO YA ACABÓ! -> Auto-Desbaneo
                        LevantarCastigo(datosUsuario.Id);
                        LogManager.RegistrarAccion("SISTEMA", "AUTO_UNBAN", $"El castigo de {usuario} ha expirado. Cuenta reactivada.");
                        // Continuamos para verificar la contraseña...
                    }
                }

                // 2. VERIFICAR CONTRASEÑA
                string passHashIngresada = GenerarHash(password);

                if (datosUsuario.PasswordHash == passHashIngresada)
                {
                    // ¡ÉXITO!
                    ResetearIntentos(datosUsuario.Id); // Práctica 2: Limpiamos intentos fallidos
                    LogManager.RegistrarAccion(usuario, "LOGIN_EXITO", "Inicio de sesión correcto");
                    return "OK";
                }
                else
                {
                    // CONTRASEÑA MAL
                    AumentarIntentos(datosUsuario.Id);
                    LogManager.RegistrarAccion(usuario, "LOGIN_FAIL", "Contraseña incorrecta");
                    return "PASS_INCORRECTA";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("ERROR REAL: " + ex.Message);
                return "ERROR_CONEXION";
            }
            finally
            {
                dbConnection.CloseConnection();
            }
        }

        // --- OBTENER DATOS COMPLETOS (Para rellenar la clase Usuario tras el login) ---
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
                                FinBaneo = reader.IsDBNull(reader.GetOrdinal("fin_baneo")) ? (DateTime?)null : reader.GetDateTime("fin_baneo")
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
                    // Por defecto: Rol 'usuario', Estado 'activo'
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

        // --- MÉTODOS AUXILIARES PRIVADOS ---

        private void LevantarCastigo(int idUsuario)
        {
            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "UPDATE usuarios SET estado_cuenta = 'activo', fin_baneo = NULL, motivo_baneo = NULL WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", idUsuario);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void AumentarIntentos(int idUsuario)
        {
            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "UPDATE usuarios SET intentos_fallidos = intentos_fallidos + 1 WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", idUsuario);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void ResetearIntentos(int idUsuario)
        {
            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "UPDATE usuarios SET intentos_fallidos = 0 WHERE id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", idUsuario);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private bool VerificarSiUsuarioExiste(string usuario)
        {
            try
            {
                using (MySqlConnection conn = dbConnection.GetConnection())
                {
                    string query = "SELECT COUNT(*) FROM usuarios WHERE nombre_usuario = @user";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", usuario);
                    return (long)cmd.ExecuteScalar() > 0;
                }
            }
            catch { return false; }
        }

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