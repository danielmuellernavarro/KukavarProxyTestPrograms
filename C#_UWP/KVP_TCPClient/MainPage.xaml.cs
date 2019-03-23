using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace KVP_TCPClient
{
    public sealed partial class MainPage : Page
    {
        Kukavarproxy kuka = new Kukavarproxy(1, 1000); // Msg ID from 1 to 1000

        public MainPage()
        {
            InitializeComponent();
            TextIp.Text = "192.168.178.24";
            TextReadInput.Text = "$AXIS_ACT";
            TextWriteOutput.Text = "";
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            Kukavarproxy.Result result = new Kukavarproxy.Result();
            string IP = TextIp.Text;
            TextStatus.Text = "Connecting";
            result = await kuka.Connect("7000", TextIp.Text);
            if (IP == TextIp.Text)
            {
                if (result.Succeeded) TextStatus.Text = "Connected";
                else TextStatus.Text = "Error: " + result.ErrorMessage;
            }
        }

        private async void ButtonRead_Click(object sender, RoutedEventArgs e)
        {
            Kukavarproxy.Result result = new Kukavarproxy.Result();
            result = await kuka.ReadVariable(TextReadInput.Text);
            if (result.Succeeded) TextReadOutput.Text = result.Data;
            else TextReadOutput.Text = "Error: " + result.ErrorMessage;
        }

        private async void ButtonWrite_Click(object sender, RoutedEventArgs e)
        {
            Kukavarproxy.Result result = new Kukavarproxy.Result();
            result = await kuka.ReadVariable(TextReadInput.Text);
            result = await kuka.WriteVariable(TextReadInput.Text, TextWriteValue.Text);
            if (result.Succeeded) TextWriteOutput.Text = result.Data;
            else TextWriteOutput.Text = "Error: " + result.ErrorMessage;
        }
    }
}
