namespace GameLauncher
{
    public class Juego
    {
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public string? RutaImagen { get; set; }
        public string? RutaBanner { get; set; }
        public string? Genero { get; set; }
        public bool EstaInstalado { get; set; }
        public string? Tamano { get; set; }
        public string? Version { get; set; }
        public int Puntuacion { get; set; }

        // NUEVO DATO
        public double HorasJugadas { get; set; }
    }
}