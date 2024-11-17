using Microsoft.Maui.Controls;
using YogaCustomerApp.Views;

namespace YogaCustomerApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();  // Initializes the components defined in MainPage.xaml
        }

        // Navigate to the "ViewAllClass" page
        private async void OnViewAllClassesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ViewAllClass());  // Navigate to ViewAllClass page
        }

        // Navigate to the "My Classes" page
        private async void OnMyClassesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MyClassesPage());
        }
    }
}
