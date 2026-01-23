using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameLauncher; // Asegúrate de que esto reconoce tu proyecto principal

namespace GameLauncher.Tests
{
    [TestClass]
    public class TestRoles
    {
        // PRUEBA 1: Verificar que un usuario normal NO se detecta como Admin
        // Corresponde a tu Plan de Pruebas: CP-002 (lógica interna)
        [TestMethod]
        public void Test_UsuarioNormal_NoEsAdmin()
        {
            // ARRANGE (Preparamos un usuario simulado como si viniera de la BBDD)
            Usuario user = new Usuario
            {
                Nombre = "Pepito",
                Rol = "usuario"
            };

            // ACT (Preguntamos)
            bool esAdmin = user.EsAdmin();

            // ASSERT (Verificamos)
            Assert.IsFalse(esAdmin, "ERROR: El sistema dice que un 'usuario' es admin, y no debería.");
        }

        // PRUEBA 2: Verificar que un Admin SÍ se detecta como Admin
        // Corresponde a tu Plan de Pruebas: CP-001 (lógica interna)
        [TestMethod]
        public void Test_UsuarioAdmin_EsAdmin()
        {
            // ARRANGE
            Usuario admin = new Usuario
            {
                Nombre = "Jefe",
                Rol = "admin"
            };

            // ACT
            bool esAdmin = admin.EsAdmin();

            // ASSERT
            Assert.IsTrue(esAdmin, "ERROR: El sistema no reconoce el rol 'admin'.");
        }

        // PRUEBA 3: Robustez (Mayúsculas/Minúsculas)
        // Evita fallos tontos si en la BBDD alguien escribió "ADMIN" o "Admin"
        [TestMethod]
        public void Test_Rol_InsensibleMayusculas()
        {
            Usuario admin = new Usuario { Nombre = "Jefe", Rol = "ADMIN" };
            Assert.IsTrue(admin.EsAdmin(), "ERROR: Debería reconocer 'ADMIN' en mayúsculas también.");
        }
    }
}