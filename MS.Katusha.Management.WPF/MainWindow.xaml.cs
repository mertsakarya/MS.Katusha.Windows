using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MS.Katusha.SDK;
using MS.Katusha.SDK.Services;

namespace MS.Katusha.Management.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UsernameTextbox.Text = "mertiko";
            PasswordTextbox.Text = "690514";

            foreach (var item in MSKatushaWinFormsConfiguration.Servers)
                ServerCombo.Items.Add(item);
            ServerCombo.SelectedIndex = 0;

            foreach (var bucket in MSKatushaWinFormsConfiguration.Buckets)
                S3Combo.Items.Add(new S3FS(bucket));
            S3Combo.SelectedIndex = 0;
            //ProfileList.VirtualMode = true;
            // S3Combo.SelectedIndex = 0;

            //button7.Enabled = !checkBox2.Checked;
            //comboBox6.Text = MSKatushaWinFormsConfiguration.Servers[0];
            //comboBox6.Items.AddRange(MSKatushaWinFormsConfiguration.Servers);
            //comboBox3.Text = Folders[0];
            //comboBox3.Items.AddRange(Folders);
            //comboBox5.Text = Folders[1];
            //comboBox5.Items.AddRange(Folders);
            //FillFiles();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearCache_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OpenDataFolder_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TextBlock_Initialized(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
