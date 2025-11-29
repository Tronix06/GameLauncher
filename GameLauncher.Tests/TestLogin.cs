#nullable disable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameLauncher;
using System.IO;
using System.Threading; // Necesario para pequeños descansos si el disco va lento

namespace GameLauncher.Tests
{
    // ESTA LÍNEA ES LA SOLUCIÓN: Obliga a que las pruebas vayan en fila india
    [DoNotParallelize]
    [TestClass]
    public class TestLogin
    {
        private const string ArchivoTest = "usuarios.xml";

        [TestInitialize]
        public void Setup()
        {
            // Intentamos borrar el archivo. Si está bloqueado por milisegundos, esperamos.
            // Esto hace la prueba mucho más robusta.
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(ArchivoTest)) File.Delete(ArchivoTest);
                    break; // Si borra bien, salimos del bucle
                }
                catch (IOException)
                {
                    Thread.Sleep(50); // Esperamos 50ms y reintentamos
                }
            }

            // Creamos el usuario base
            GestorUsuarios gestor = new GestorUsuarios();
            gestor.RegistrarUsuario("admin", "1234", "admin@test.com");
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Opcional: Limpiar después de cada test también
            try { if (File.Exists(ArchivoTest)) File.Delete(ArchivoTest); } catch { }
        }

        // PRUEBA 1: Login Correcto
        [TestMethod]
        public void Test_Login_Exitoso()
        {
            GestorUsuarios gestor = new GestorUsuarios();
            string resultado = gestor.ValidarLogin("admin", "1234");
            Assert.AreEqual("OK", resultado, "El login debería ser exitoso.");
        }

        // PRUEBA 2: Contraseña Incorrecta
        [TestMethod]
        public void Test_Login_ContrasenaIncorrecta()
        {
            GestorUsuarios gestor = new GestorUsuarios();
            string resultado = gestor.ValidarLogin("admin", "clave_falsa");
            Assert.AreEqual("PASS_INCORRECTA", resultado);
        }

        // PRUEBA 3: Usuario No Existe
        [TestMethod]
        public void Test_Login_UsuarioNoExiste()
        {
            GestorUsuarios gestor = new GestorUsuarios();
            string resultado = gestor.ValidarLogin("usuario_fantasma", "1234");
            Assert.AreEqual("NO_EXISTE", resultado);
        }

        // PRUEBA 4: Bloqueo tras 3 intentos
        [TestMethod]
        public void Test_Bloqueo_Seguridad()
        {
            GestorUsuarios gestor = new GestorUsuarios();

            // Fallo 1
            gestor.ValidarLogin("admin", "mal");
            // Fallo 2
            gestor.ValidarLogin("admin", "mal");
            // Fallo 3 -> Bloqueo
            string resultado = gestor.ValidarLogin("admin", "mal");

            Assert.AreEqual("BLOQUEADO_AHORA", resultado);
        }

        // PRUEBA 5: Registro de Nuevo Usuario
        [TestMethod]
        public void Test_Registro_Nuevo()
        {
            GestorUsuarios gestor = new GestorUsuarios();

            bool registroExito = gestor.RegistrarUsuario("nuevoUser", "abc", "test@mail.com");

            Assert.IsTrue(registroExito, "El registro debería funcionar.");

            // Verificamos login
            string login = gestor.ValidarLogin("nuevoUser", "abc");
            Assert.AreEqual("OK", login);
        }
    }
}