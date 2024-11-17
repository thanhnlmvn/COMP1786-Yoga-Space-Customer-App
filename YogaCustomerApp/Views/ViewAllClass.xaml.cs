using Microsoft.Maui.Controls;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace YogaCustomerApp.Views
{
    public partial class ViewAllClass : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;

        // ObservableCollection to hold yoga classes
        public ObservableCollection<YogaClass> YogaClasses { get; set; }
        private ObservableCollection<YogaClass> AllYogaClasses { get; set; }  // Store all classes for searching
        public ObservableCollection<string> TeacherSuggestions { get; set; } // Suggestions for teacher names
        public ObservableCollection<YogaClass> CartClasses { get; set; } = new ObservableCollection<YogaClass>(); // Cart for storing added classes

        public ViewAllClass()
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://yogaapp-b76cc-default-rtdb.firebaseio.com/");
            LoadYogaClasses(); // Load classes when the page is initialized
        }

        private async void LoadYogaClasses()
        {
            // Fetch list of yoga classes from Firebase
            AllYogaClasses = await GetYogaClasses();
            YogaClasses = new ObservableCollection<YogaClass>(AllYogaClasses);

            // Get distinct teacher names for suggestions
            TeacherSuggestions = new ObservableCollection<string>(AllYogaClasses.Select(x => x.TeacherName).Distinct());

            // Bind data to the UI
            BindingContext = this;
        }

        public async Task<ObservableCollection<YogaClass>> GetYogaClasses()
        {
            var classes = await _firebaseClient
                .Child("classes")
                .OnceAsync<YogaClass>();

            List<YogaClass> yogaClassList = new List<YogaClass>();
            foreach (var yogaClass in classes)
            {
                yogaClassList.Add(yogaClass.Object);
            }
            return new ObservableCollection<YogaClass>(yogaClassList);
        }

        private void OnTeacherSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLower();

            TeacherSuggestions = new ObservableCollection<string>(AllYogaClasses
                .Where(x => x.TeacherName.ToLower().Contains(searchText))
                .Select(x => x.TeacherName)
                .Distinct());

            OnPropertyChanged(nameof(TeacherSuggestions));

            // Filter classes by teacher name
            YogaClasses = new ObservableCollection<YogaClass>(AllYogaClasses
                .Where(x => x.TeacherName.ToLower().Contains(searchText)));

            OnPropertyChanged(nameof(YogaClasses));
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            var selectedDate = e.NewDate.ToString("dddd, dd/MM/yyyy");

            YogaClasses = new ObservableCollection<YogaClass>(AllYogaClasses
                .Where(x => x.Date == selectedDate));

            OnPropertyChanged(nameof(YogaClasses));
        }

        private void OnTeacherSuggestionTapped(object sender, ItemTappedEventArgs e)
        {
            var selectedTeacher = e.Item as string;
            if (selectedTeacher != null)
            {
                YogaClasses = new ObservableCollection<YogaClass>(AllYogaClasses
                    .Where(x => x.TeacherName.ToLower() == selectedTeacher.ToLower()));

                OnPropertyChanged(nameof(YogaClasses));
                TeacherSuggestionsListView.IsVisible = false;
            }
        }

        // Add class to the cart and update the "Your Cart" button
        private void OnAddToCartClicked(object sender, EventArgs e)
        {
            var tappedButton = sender as Button;
            var selectedClass = tappedButton?.BindingContext as YogaClass;

            if (selectedClass != null)
            {
                if (!CartClasses.Contains(selectedClass))
                {
                    CartClasses.Add(selectedClass);
                }

                // Update the "Your Cart" button text
                UpdateCartButton();
            }
        }

        // Update the cart button with the number of items in the cart
        private void UpdateCartButton()
        {
            YourCartButton.Text = $"Your Cart ({CartClasses.Count})";
        }

        // Navigate to the cart page
        private async void OnYourCartClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CartPage(CartClasses));
        }

        // Navigate to the class detail page
        private async void OnDetailClicked(object sender, EventArgs e)
        {
            var tappedButton = sender as Button;
            var selectedClass = tappedButton?.BindingContext as YogaClass;

            if (selectedClass != null)
            {
                await Navigation.PushAsync(new ClassDetail(selectedClass));
            }
        }
    }

    public class YogaClass
    {
        public int Capacity { get; set; }
        public string ClassType { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public int Id { get; set; }
        public int Price { get; set; }
        public string TeacherName { get; set; }
        public string Time { get; set; }
        public string FirebaseId { get; set; }
        public List<string> BookedUsers { get; set; }
    }
}
