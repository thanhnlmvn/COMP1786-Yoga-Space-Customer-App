using Microsoft.Maui.Controls;

namespace YogaCustomerApp.Views
{
    public partial class ClassDetail : ContentPage
    {
        public ClassDetail(YogaClass yogaClass)
        {
            InitializeComponent();
            BindingContext = yogaClass;  // Gán lớp học vào BindingContext để hiển thị thông tin chi tiết
        }

        // Xử lý sự kiện khi nhấn nút "Back"
        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Quay lại trang trước đó (ViewAllClass)
            await Navigation.PopAsync();
        }
    }
}
