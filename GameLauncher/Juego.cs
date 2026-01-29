namespace GameLauncher
{
    public class Juego
    {
        // ==========================================
        // 1. DATOS DE IDENTIDAD (BBDD)
        // ==========================================
        public int Id { get; set; } // La Primary Key de MySQL

        // ==========================================
        // 2. DATOS DE LA TIENDA (CATÁLOGO)
        // ==========================================
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? Genero { get; set; }

        // La URL de la imagen o la ruta local (servirá para tu componente visual)
        public string? RutaImagen { get; set; }

        public decimal Precio { get; set; } // NUEVO: Requisito P5
        public bool EsVisible { get; set; } = true; // NUEVO: Para gestión del Admin

        // ==========================================
        // 3. DATOS DE LA BIBLIOTECA (USUARIO)
        // ==========================================
        // Estos datos cambiarán dependiendo de si el usuario ha comprado/instalado el juego

        public bool EstaInstalado { get; set; } // MANTENIDO: Para botón Instalar/Jugar
        public double HorasJugadas { get; set; } // MANTENIDO: Ahora se guarda en BBDD
        public bool EsFavorito { get; set; }    // NUEVO: Filtro de biblioteca

        // ==========================================
        // 4. DATOS LEGACY / EXTRAS (Mantenidos)
        // ==========================================
        // Los mantenemos para compatibilidad con tus componentes anteriores.
        // (Aunque no estén todos en BBDD, evitamos que el código falle)
        public string? RutaBanner { get; set; }
        public string? Tamano { get; set; }
        public string? Version { get; set; }
        public int Puntuacion { get; set; }
        public DateTime? FechaLanzamiento { get; set; }
    }
}