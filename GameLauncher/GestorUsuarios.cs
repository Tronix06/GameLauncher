using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace GameLauncher
{
    public class GestorUsuarios
    {
        private string rutaXml = "usuarios.xml";

        // Encriptar contraseña
        public static string Encriptar(string texto)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Cargar usuarios del XML
        public List<Usuario> CargarUsuarios()
        {
            if (!File.Exists(rutaXml)) return new List<Usuario>();

            try
            {
                XDocument doc = XDocument.Load(rutaXml);
                var lista = new List<Usuario>();

                foreach (var elemento in doc.Descendants("Usuario"))
                {
                    Usuario u = new Usuario();
                    u.Nombre = elemento.Element("Nombre")?.Value;
                    u.PasswordHash = elemento.Element("Password")?.Value;
                    u.Email = elemento.Element("Email")?.Value ?? "";

                    int intentos = 0;
                    int.TryParse(elemento.Element("Intentos")?.Value, out intentos);
                    u.IntentosFallidos = intentos;

                    string bloqueoStr = elemento.Element("Bloqueo")?.Value;
                    if (!string.IsNullOrEmpty(bloqueoStr))
                    {
                        u.FechaDesbloqueo = DateTime.Parse(bloqueoStr);
                    }

                    lista.Add(u);
                }
                return lista;
            }
            catch
            {
                return new List<Usuario>();
            }
        }

        // Guardar usuarios en el XML
        public void GuardarUsuarios(List<Usuario> listaUsuarios)
        {
            XDocument doc = new XDocument(new XElement("Usuarios"));

            foreach (var u in listaUsuarios)
            {
                XElement nodo = new XElement("Usuario",
                    new XElement("Nombre", u.Nombre),
                    new XElement("Password", u.PasswordHash),
                    new XElement("Email", u.Email),
                    new XElement("Intentos", u.IntentosFallidos),
                    new XElement("Bloqueo", u.FechaDesbloqueo?.ToString() ?? "")
                );
                doc.Root.Add(nodo);
            }

            doc.Save(rutaXml);
        }

        // Lógica de Login (Esto es lo que probaremos)
        public string ValidarLogin(string usuario, string passwordPlana)
        {
            var usuarios = CargarUsuarios();
            var userObj = usuarios.FirstOrDefault(u => u.Nombre == usuario);

            if (userObj == null) return "NO_EXISTE";

            if (userObj.EstaBloqueado())
            {
                double minutos = (userObj.FechaDesbloqueo.Value - DateTime.Now).TotalMinutes;
                return $"BLOQUEADO|{Math.Ceiling(minutos)}";
            }

            string hashIntroducido = Encriptar(passwordPlana);

            if (userObj.PasswordHash == hashIntroducido)
            {
                userObj.IntentosFallidos = 0;
                userObj.FechaDesbloqueo = null;
                GuardarUsuarios(usuarios);
                return "OK";
            }
            else
            {
                userObj.IntentosFallidos++;

                if (userObj.IntentosFallidos >= 3)
                {
                    userObj.FechaDesbloqueo = DateTime.Now.AddMinutes(5);
                    GuardarUsuarios(usuarios);
                    return "BLOQUEADO_AHORA";
                }

                GuardarUsuarios(usuarios);
                return "PASS_INCORRECTA";
            }
        }

        // Registrar nuevo usuario
        public bool RegistrarUsuario(string nombre, string passPlana, string email)
        {
            var usuarios = CargarUsuarios();

            if (usuarios.Any(u => u.Nombre == nombre)) return false;

            Usuario nuevo = new Usuario
            {
                Nombre = nombre,
                PasswordHash = Encriptar(passPlana),
                Email = email,
                IntentosFallidos = 0
            };

            usuarios.Add(nuevo);
            GuardarUsuarios(usuarios);
            return true;
        }
    }
}