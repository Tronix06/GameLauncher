using System;
using MySql.Data.MySqlClient;
using System.Windows;

namespace GameLauncher
{
    public class DatabaseConnection
    {
        // --------------------------------------------------------------------------------------
        // ⚠️ NOTA: Asegúrate de que "Pwd" es tu contraseña real de MySQL
        // --------------------------------------------------------------------------------------
        private string connectionString = "Server=localhost;Database=bitronix_db;Uid=root;Pwd=root;";

        // ESTE ERA EL ERROR: Antes guardábamos la 'connection' aquí como variable de clase.
        // Al quitarla de aquí y crearla abajo, solucionamos el "ObjectDisposedException".

        public MySqlConnection GetConnection()
        {
            try
            {
                // SOLUCIÓN: Creamos una conexión NUEVA cada vez que se llama al método
                MySqlConnection connection = new MySqlConnection(connectionString);
                connection.Open(); // La abrimos antes de entregarla
                return connection;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error conectando a MySQL: " + ex.Message);
                return null;
            }
        }

        // Este método ya no hace falta que cierre nada porque el 'using' 
        // del GestorUsuarios se encarga de matar la conexión nueva,
        // pero lo dejamos vacío por si lo llamas desde algún sitio antiguo para que no de error.
        public void CloseConnection()
        {
            // No hacemos nada, la conexión se cierra sola al terminar el 'using'
        }

        // Método para probar si conecta (puedes usarlo en el constructor de MainWindow si quieres)
        public void ProbarConexion()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Si llega aquí, todo bien
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("PRUEBA DE CONEXIÓN FALLIDA: " + ex.Message);
            }
        }
    }
}