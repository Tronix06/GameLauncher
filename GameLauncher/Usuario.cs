using System;

namespace GameLauncher
{
    public class Usuario
    {
        public string? Nombre { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }

        // Esto es vital para la prueba de bloqueo
        public int IntentosFallidos { get; set; }
        public DateTime? FechaDesbloqueo { get; set; }

        public bool EstaBloqueado()
        {
            if (FechaDesbloqueo != null && FechaDesbloqueo > DateTime.Now)
            {
                return true;
            }
            return false;
        }
    }
}