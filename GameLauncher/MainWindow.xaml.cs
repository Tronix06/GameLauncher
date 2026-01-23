#nullable disable
using System;
using System.IO; // Para guardar el "Recordarme"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation; // Para las animaciones

namespace GameLauncher
{
    public partial class MainWindow : Window
    {
        private GestorUsuarios gestor = new GestorUsuarios();

        // Colores
        private Brush activeTabColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0074E0"));
        private Brush inactiveTabColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#777777"));

        // Archivo para guardar usuario
        private string archivoConfig = "config.ini";

        public MainWindow()
        {
            InitializeComponent();
            // PRUEBA TEMPORAL DE CONEXIÓN
            DatabaseConnection pruebaDB = new DatabaseConnection();
            pruebaDB.ProbarConexion(); // Esto lanzará el MessageBox de éxito o error que programamos

            double altoPantalla = SystemParameters.PrimaryScreenHeight;
            this.Height = 600;
            this.Width = 400;

            // AL ARRANCAR: Comprobamos si hay alguien para recordar
            CargarPreferencias();
        }

        // --- MOVIMIENTO Y CIERRE ---
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnSalir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // --- PESTAÑAS CON ANIMACIÓN ---
        private void BtnTabLogin_Click(object sender, RoutedEventArgs e)
        {
            CambiarPestana(true);
        }

        private void BtnTabRegistro_Click(object sender, RoutedEventArgs e)
        {
            CambiarPestana(false);
        }

        private void CambiarPestana(bool esLogin)
        {
            if (PanelLogin == null || PanelRegistro == null) return;

            if (esLogin)
            {
                // Animación: Registro desaparece, Login aparece
                if (PanelLogin.Visibility != Visibility.Visible)
                {
                    AnimateOpacity(PanelRegistro, 1, 0, () =>
                    {
                        PanelRegistro.Visibility = Visibility.Collapsed;
                        PanelLogin.Visibility = Visibility.Visible;
                        AnimateOpacity(PanelLogin, 0, 1);
                    });
                }

                // Estilos botones
                btnTabLogin.Foreground = activeTabColor;
                btnTabLogin.BorderThickness = new Thickness(0, 0, 0, 2);
                btnTabLogin.BorderBrush = activeTabColor;

                btnTabRegistro.Foreground = inactiveTabColor;
                btnTabRegistro.BorderThickness = new Thickness(0);

                lblMensajeLogin.Text = "";
            }
            else
            {
                // Animación: Login desaparece, Registro aparece
                if (PanelRegistro.Visibility != Visibility.Visible)
                {
                    AnimateOpacity(PanelLogin, 1, 0, () =>
                    {
                        PanelLogin.Visibility = Visibility.Collapsed;
                        PanelRegistro.Visibility = Visibility.Visible;
                        AnimateOpacity(PanelRegistro, 0, 1);
                    });
                }

                // Estilos botones
                btnTabRegistro.Foreground = activeTabColor;
                btnTabRegistro.BorderThickness = new Thickness(0, 0, 0, 2);
                btnTabRegistro.BorderBrush = activeTabColor;

                btnTabLogin.Foreground = inactiveTabColor;
                btnTabLogin.BorderThickness = new Thickness(0);

                lblMensajeRegistro.Text = "";
            }
        }

        private void AnimateOpacity(UIElement target, double from, double to, Action onCompleted = null)
        {
            DoubleAnimation anim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(200));
            if (onCompleted != null)
            {
                anim.Completed += (s, e) => onCompleted();
            }
            target.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        
        // --- LOGIN INTELIGENTE V2.0 (SaaS & Roles) ---
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuarioLogin.Text;
            string pass = txtPasswordLoginVisible.Visibility == Visibility.Visible ? txtPasswordLoginVisible.Text : txtPasswordLogin.Password;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass))
            {
                lblMensajeLogin.Text = "⚠️ Introduce tus credenciales";
                lblMensajeLogin.Foreground = Brushes.OrangeRed;
                return;
            }

            // Llamamos al nuevo validador inteligente
            string resultado = gestor.ValidarLogin(usuario, pass);

            if (resultado == "OK")
            {
                // 1. Recuperamos el "Pasaporte Completo" (Rol, ID, Email...)
                Usuario datosUsuario = gestor.ObtenerDatosUsuario(usuario);

                if (datosUsuario != null)
                {
                    lblMensajeLogin.Text = "✅ Conectando...";
                    lblMensajeLogin.Foreground = Brushes.LightGreen;

                    GuardarPreferencias(usuario);

                    // 2. Pasamos el OBJETO COMPLETO a la Home (No solo el string)
                    Home home = new Home(datosUsuario);
                    home.Show();
                    this.Close();
                }
            }
            // --- MANEJO DE ERRORES PROFESIONALES ---
            // ... (dentro de BtnLogin_Click) ...

            else if (resultado == "CUENTA_SUSPENDIDA" || resultado == "CUENTA_BANEADA_TEMP")
            {
                // 1. Recuperamos los datos del usuario para saber por qué está castigado
                Usuario datosBaneado = gestor.ObtenerDatosUsuario(usuario);

                // 2. Abrimos la VENTANA PERSONALIZADA DE CASTIGO
                CastigoWindow ventanaCastigo = new CastigoWindow(datosBaneado);
                ventanaCastigo.Show();

                // 3. Cerramos el Login
                this.Close();
            }

            // ... (resto de errores NO_EXISTE, PASS_INCORRECTA siguen igual) ...
            else if (resultado == "NO_EXISTE")
            {
                lblMensajeLogin.Text = "❌ Usuario no encontrado";
                lblMensajeLogin.Foreground = Brushes.OrangeRed;
            }
            else if (resultado == "PASS_INCORRECTA")
            {
                lblMensajeLogin.Text = "❌ Contraseña incorrecta";
                lblMensajeLogin.Foreground = Brushes.OrangeRed;
            }
            else
            {
                lblMensajeLogin.Text = "⚠️ Error de conexión";
                lblMensajeLogin.Foreground = Brushes.OrangeRed;
            }
        }

        // --- REGISTRO ---
        private void BtnRegistro_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuarioRegistro.Text;
            string email = txtEmailRegistro.Text;
            string pass = txtPasswordRegistroVisible.Visibility == Visibility.Visible ? txtPasswordRegistroVisible.Text : txtPasswordRegistro.Password;

            // Validaciones
            if (string.IsNullOrWhiteSpace(usuario))
            {
                lblMensajeRegistro.Text = "⚠️ Falta el nombre de usuario";
                lblMensajeRegistro.Foreground = Brushes.OrangeRed;
                txtUsuarioRegistro.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
            {
                lblMensajeRegistro.Text = "⚠️ Email incorrecto";
                lblMensajeRegistro.Foreground = Brushes.OrangeRed;
                txtEmailRegistro.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(pass))
            {
                lblMensajeRegistro.Text = "⚠️ Falta la contraseña";
                lblMensajeRegistro.Foreground = Brushes.OrangeRed;
                txtPasswordRegistro.Focus();
                return;
            }

            bool exito = gestor.RegistrarUsuario(usuario, pass, email);
            if (exito)
            {
                MessageBox.Show("¡Cuenta creada con éxito!\nBienvenido a la familia BiTronix. Por favor, inicia sesión.", "BiTronix", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar
                txtUsuarioRegistro.Clear();
                txtEmailRegistro.Clear();
                txtPasswordRegistro.Clear();
                lblMensajeRegistro.Text = "";

                // Cambiar a pestaña Login
                CambiarPestana(true);
                txtUsuarioLogin.Text = usuario;
                txtPasswordLogin.Focus();
            }
            else
            {
                lblMensajeRegistro.Text = "❌ Ese usuario ya existe";
                lblMensajeRegistro.Foreground = Brushes.OrangeRed;
            }
        }

        // --- LÓGICA DEL OJO (VER CONTRASEÑA) ---
        private void TogglePassLogin_Click(object sender, RoutedEventArgs e)
        {
            if (txtPasswordLoginVisible.Visibility == Visibility.Collapsed)
            {
                txtPasswordLoginVisible.Text = txtPasswordLogin.Password;
                txtPasswordLoginVisible.Visibility = Visibility.Visible;
                txtPasswordLogin.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPasswordLogin.Password = txtPasswordLoginVisible.Text;
                txtPasswordLogin.Visibility = Visibility.Visible;
                txtPasswordLoginVisible.Visibility = Visibility.Collapsed;
            }
        }

        private void TogglePassRegistro_Click(object sender, RoutedEventArgs e)
        {
            if (txtPasswordRegistroVisible.Visibility == Visibility.Collapsed)
            {
                txtPasswordRegistroVisible.Text = txtPasswordRegistro.Password;
                txtPasswordRegistroVisible.Visibility = Visibility.Visible;
                txtPasswordRegistro.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPasswordRegistro.Password = txtPasswordRegistroVisible.Text;
                txtPasswordRegistro.Visibility = Visibility.Visible;
                txtPasswordRegistroVisible.Visibility = Visibility.Collapsed;
            }
        }

        // --- SISTEMA DE "RECORDARME" ---
        private void CargarPreferencias()
        {
            try
            {
                if (File.Exists(archivoConfig))
                {
                    string usuarioGuardado = File.ReadAllText(archivoConfig);
                    if (!string.IsNullOrEmpty(usuarioGuardado))
                    {
                        txtUsuarioLogin.Text = usuarioGuardado;
                        chkRecordarme.IsChecked = true;
                    }
                }
            }
            catch { }
        }

        private void GuardarPreferencias(string usuario)
        {
            try
            {
                if (chkRecordarme.IsChecked == true)
                {
                    File.WriteAllText(archivoConfig, usuario);
                }
                else
                {
                    if (File.Exists(archivoConfig)) File.Delete(archivoConfig);
                }
            }
            catch { }
        }

        // --- TECLA ENTER ---
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (PanelLogin.Visibility == Visibility.Visible)
                {
                    BtnLogin_Click(sender, e);
                }
                else
                {
                    BtnRegistro_Click(sender, e);
                }
            }
        }

        // --- MENSAJES WIP (Tus textos) ---
        private void WIP_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🚧 Esta funcionalidad está en desarrollo.\n\nPróximamente en BiTronix 2.0.", "En Construcción", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WIP_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("🚧 El sistema de recuperación de contraseña estará disponible en BiTronix 2.0.", "En Construcción", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}