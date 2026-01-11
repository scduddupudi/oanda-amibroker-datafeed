// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginBox.xaml.cs" company="Clartix LLC">
//   Copyright © 2026 Sanjay DUDDUPUDI, Clartix LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin.Controls
{
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for LoginBox.xaml
    /// </summary>
    public partial class LoginBox : UserControl
    {
        private readonly DataSource dataSource;

        public LoginBox(DataSource dataSource)
        {
            this.dataSource = dataSource;
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Hello " + txtUserName.Text);
        }
    }
}
