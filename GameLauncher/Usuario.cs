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

        // --- NUEVO P5: PERMISOS GRANULARES DE ADMIN ---
        // 'superadmin', 'admin_total', 'admin_usuarios', 'admin_juegos'
        public string TipoAdmin { get; set; }

        public string PasswordHash { get; set; }

        // --- SISTEMA DE BANEO AVANZADO (SaaS) ---
        public string EstadoCuenta { get; set; }
        public string MotivoBaneo { get; set; }
        public DateTime? FinBaneo { get; set; }

        // --- SISTEMA DE SEGURIDAD (Anti Brute-Force) ---
        public int IntentosFallidos { get; set; }
        public DateTime? FechaDesbloqueo { get; set; }

        // --- HELPERS ---
        public bool EsAdmin()
        {
            return !string.IsNullOrEmpty(Rol) && Rol.ToLower() == "admin";
        }

        // --- NUEVO HELPER P5: ¿PUEDE VER PESTAÑA JUEGOS? ---
        public bool PuedeGestionarJuegos()
        {
            if (!EsAdmin()) return false;
            // Solo si es SuperAdmin, Total o Juegos
            return TipoAdmin == "superadmin" || TipoAdmin == "admin_total" || TipoAdmin == "admin_juegos";
        }

        // --- NUEVO HELPER P5: ¿PUEDE VER PESTAÑA USUARIOS? ---
        public bool PuedeGestionarUsuarios()
        {
            if (!EsAdmin()) return false;
            // Solo si es SuperAdmin, Total o Usuarios
            return TipoAdmin == "superadmin" || TipoAdmin == "admin_total" || TipoAdmin == "admin_usuarios";
        }

        public string ObtenerTiempoRestanteBaneo()
        {
            if (FinBaneo.HasValue && FinBaneo.Value > DateTime.Now)
            {
                TimeSpan restante = FinBaneo.Value - DateTime.Now;
                if (restante.TotalDays >= 1) return $"{(int)restante.TotalDays} días";
                if (restante.TotalHours >= 1) return $"{(int)restante.TotalHours} horas";
                return $"{(int)restante.TotalMinutes} minutos";
            }
            return "0 minutos";
        }
    }
}