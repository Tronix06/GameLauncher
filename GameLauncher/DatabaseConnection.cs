using MySql.Data.MySqlClient;
using System;
using System.Windows; // Para los MessageBox

namespace GameLauncher
{
    public class DatabaseConnection
    {
        // CAMBIA ESTO CON TUS DATOS REALES DE MYSQL
        private string connectionString = "Server=localhost;Database=bitronix_db;Uid=root;Pwd=root;";
        private MySqlConnection connection;

        public DatabaseConnection()
        {
            connection = new MySqlConnection(connectionString);
        }

        // Método para probar si la conexión funciona (Vital para el punto 1 de la rúbrica)
        public bool ProbarConexion()
        {
            try
            {
                connection.Open();
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error de conexión a la base de datos: " + ex.Message, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // Método para obtener la conexión abierta (lo usaremos en Login y Registro)
        public MySqlConnection GetConnection()
        {
            if (connection.State == System.Data.ConnectionState.Closed || connection.State == System.Data.ConnectionState.Broken)
            {
                connection.Open();
            }
            return connection;
        }

        // Método para cerrar (buena práctica)
        public void CloseConnection()
        {
            if (connection.State == System.Data.ConnectionState.Open)
                connection.Close();
        }
    }
}