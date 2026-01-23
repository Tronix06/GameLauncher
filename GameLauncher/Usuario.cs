using System;

namespace GameLauncher
{
    public class Usuario
    {
        // --- DATOS BÁSICOS ---
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Rol { get; set; } = "usuario"; // 'admin' o 'usuario'
        public string PasswordHash { get; set; }

        // --- SISTEMA DE BANEO AVANZADO (SaaS) ---
        // Estado: 'activo', 'baneado', 'suspendido'
        public string EstadoCuenta { get; set; }
        public string MotivoBaneo { get; set; }
        public DateTime? FinBaneo { get; set; } // ¿Cuándo acaba el castigo?

        // --- SISTEMA DE SEGURIDAD (Anti Brute-Force - Práctica 2) ---
        public int IntentosFallidos { get; set; }
        public DateTime? FechaDesbloqueo { get; set; }

        // --- HELPERS ---
        public bool EsAdmin()
        {
            return !string.IsNullOrEmpty(Rol) && Rol.ToLower() == "admin";
        }

        // Calcula el tiempo restante de baneo para mostrarlo en el Login
        public string ObtenerTiempoRestanteBaneo()
        {
            if (FinBaneo.HasValue && FinBaneo.Value > DateTime.Now)
            {
                TimeSpan restante = FinBaneo.Value - DateTime.Now;

                if (restante.TotalDays >= 1)
                    return $"{(int)restante.TotalDays} días";

                if (restante.TotalHours >= 1)
                    return $"{(int)restante.TotalHours} horas";

                return $"{(int)restante.TotalMinutes} minutos";
            }
            return "0 minutos";
        }
    }
}