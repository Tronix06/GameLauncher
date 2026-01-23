using System.Windows;
using System.Windows.Media;

namespace GameLauncher
{
    public partial class BiTronixMsgBox : Window
    {
        // Enumeradores para configurar la ventana fácil
        public enum Type { Info, Success, Warning, Error, Confirmation }
        public enum Buttons { Ok, YesNo }

        // Resultado para saber qué pulsó el usuario
        public bool Result { get; private set; } = false;

        // CONSTRUCTOR PRIVADO (Se usa el método estático Show)
        private BiTronixMsgBox(string mensaje, string titulo, Type tipo, Buttons botones)
        {
            InitializeComponent();
            ConfigurarVentana(mensaje, titulo, tipo, botones);
        }

        // --- MÉTODO ESTÁTICO (EL QUE USARÁS SIEMPRE) ---
        public static bool Show(string mensaje, string titulo = "BiTronix", Type tipo = Type.Info, Buttons botones = Buttons.Ok)
        {
            BiTronixMsgBox msg = new BiTronixMsgBox(mensaje, titulo, tipo, botones);
            msg.ShowDialog(); // Bloquea hasta que se cierra
            return msg.Result;
        }

        private void ConfigurarVentana(string mensaje, string titulo, Type tipo, Buttons botones)
        {
            txtMensaje.Text = mensaje;
            txtTitulo.Text = titulo.ToUpper();

            // Configurar Colores e Iconos según el Tipo
            switch (tipo)
            {
                case Type.Info:
                    BordePrincipal.BorderBrush = Brushes.DodgerBlue;
                    btnOk.Background = Brushes.DodgerBlue;
                    txtIcono.Text = "ℹ️";
                    break;
                case Type.Success:
                    BordePrincipal.BorderBrush = Brushes.LimeGreen;
                    btnOk.Background = Brushes.LimeGreen;
                    txtIcono.Text = "✅";
                    break;
                case Type.Warning:
                    BordePrincipal.BorderBrush = Brushes.Orange;
                    btnOk.Background = Brushes.Orange;
                    txtIcono.Text = "⚠️";
                    break;
                case Type.Error:
                    BordePrincipal.BorderBrush = Brushes.Red;
                    btnOk.Background = Brushes.Red;
                    txtIcono.Text = "❌";
                    break;
                case Type.Confirmation:
                    BordePrincipal.BorderBrush = Brushes.MediumPurple;
                    btnOk.Background = Brushes.MediumPurple;
                    txtIcono.Text = "❓";
                    break;
            }

            // Configurar Botones
            if (botones == Buttons.YesNo)
            {
                btnOk.Content = "SÍ, CONFIRMAR";
                btnCancel.Visibility = Visibility.Visible;
                btnCancel.Content = "CANCELAR";
            }
            else
            {
                btnOk.Content = "ENTENDIDO";
                btnCancel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            this.Close();
        }
    }
}