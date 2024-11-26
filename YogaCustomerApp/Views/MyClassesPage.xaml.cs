using Microsoft.Maui.Controls;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace YogaCustomerApp.Views
{
    public partial class MyClassesPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;

        public ObservableCollection<Booking> BookedClasses { get; set; }

        public MyClassesPage()
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://yogaapp-b76cc-default-rtdb.firebaseio.com/");
            BookedClasses = new ObservableCollection<Booking>();
            BookedClasses.CollectionChanged += OnBookedClassesChanged; // Subscribe to collection changes
            LoadBookedClasses(); // Load booked classes when page is initialized
        }

        // Event to update the total when the collection changes
        private void OnBookedClassesChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateTotalBookedClasses();
        }

        // Load booked classes from Firebase
        private async Task LoadBookedClasses()
        {
            try
            {
                var bookings = await _firebaseClient
                    .Child("bookings") // Get data from the "bookings" node
                    .OnceAsync<Booking>();

                // Populate the BookedClasses collection with the fetched bookings
                BookedClasses.Clear();
                foreach (var booking in bookings.Select(b => b.Object))
                {
                    BookedClasses.Add(booking);
                }

                BindingContext = this;

                // Update the total number of booked classes
                UpdateTotalBookedClasses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading classes: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred while loading booked classes: {ex.Message}", "OK");
            }
        }

        // Handle "Cancel Class" button click
        private async void OnCancelClassClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var booking = button.BindingContext as Booking;

            if (booking == null || string.IsNullOrEmpty(booking.ClassId) || string.IsNullOrEmpty(booking.Email))
            {
                await DisplayAlert("Error", "Invalid booking data.", "OK");
                return;
            }

            bool confirmCancel = await DisplayAlert("Cancel Class", $"Do you want to cancel the class '{booking.ClassType}'?", "Yes", "No");
            if (!confirmCancel) return;

            try
            {
                var bookingSnapshot = await _firebaseClient
                    .Child("bookings")
                    .OnceAsync<Booking>();

                var bookingToDelete = bookingSnapshot.FirstOrDefault(b =>
                    b.Object.ClassId == booking.ClassId && b.Object.Email == booking.Email);

                if (bookingToDelete == null)
                {
                    await DisplayAlert("Error", "Booking record not found.", "OK");
                    return;
                }

                // Delete booking from Firebase
                await _firebaseClient
                    .Child("bookings")
                    .Child(bookingToDelete.Key)
                    .DeleteAsync();

                // Update class in Firebase
                var firebaseClass = await _firebaseClient
                    .Child("classes")
                    .Child(booking.ClassId)
                    .OnceSingleAsync<YogaClass>();

                if (firebaseClass != null && firebaseClass.BookedUsers != null)
                {
                    firebaseClass.BookedUsers.Remove(booking.Email);

                    await _firebaseClient
                        .Child("classes")
                        .Child(booking.ClassId)
                        .Child("BookedUsers")
                        .PutAsync(firebaseClass.BookedUsers);
                }

                // Remove booking from customer data in Firebase
                var sanitizedEmail = booking.Email.Replace(".", "_");
                var customerRef = _firebaseClient
                    .Child("customers")
                    .Child(sanitizedEmail);

                await customerRef
                    .Child("BookedClasses")
                    .Child(booking.ClassId)
                    .DeleteAsync();

                // Update the ObservableCollection
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (BookedClasses.Contains(booking))
                    {
                        BookedClasses.Remove(booking);
                    }
                });

                await DisplayAlert("Success", $"Class '{booking.ClassType}' has been successfully canceled.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error canceling class: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred while canceling the class: {ex.Message}", "OK");
            }
        }

        // Update the total booked classes
        private void UpdateTotalBookedClasses()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (TotalBookedClassesLabel != null)
                {
                    TotalBookedClassesLabel.Text = BookedClasses?.Count.ToString() ?? "0";
                }
            });
        }

        // Class representing a Booking
        public class Booking
        {
            public string Email { get; set; }
            public string ClassId { get; set; }
            public string ClassType { get; set; }
            public string Date { get; set; }
            public decimal Price { get; set; }
            public string Status { get; set; }
            public string TeacherName { get; set; }
        }

        // Class representing a YogaClass
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
            public List<string> BookedUsers { get; set; } = new List<string>();
        }
    }
}
