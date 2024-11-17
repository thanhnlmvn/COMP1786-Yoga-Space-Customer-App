using Microsoft.Maui.Controls;

namespace YogaCustomerApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Đảm bảo ứng dụng sử dụng NavigationPage để hỗ trợ điều hướng
            MainPage = new NavigationPage(new MainPage());
        }
    }
}
