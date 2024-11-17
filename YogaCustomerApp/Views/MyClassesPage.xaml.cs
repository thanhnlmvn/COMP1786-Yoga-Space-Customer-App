using Microsoft.Maui.Controls;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
            LoadBookedClasses();  // Load the booked classes when page is initialized
        }

        // Load booked classes from Firebase
        private async Task LoadBookedClasses()
        {
            var bookings = await _firebaseClient
                .Child("bookings")  // Get data from the "bookings" node
                .OnceAsync<Booking>();

            // Populate the BookedClasses collection with the fetched bookings
            BookedClasses = new ObservableCollection<Booking>(bookings.Select(b => b.Object));
            BindingContext = this;

            // Update the total number of booked classes
            TotalBookedClassesLabel.Text = BookedClasses.Count.ToString();
        }


        // Handle "Cancel Class" button click
        private async void OnCancelClassClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var booking = button.BindingContext as Booking;  // Get the current booking context

            if (booking != null)
            {
                try
                {
                    // Remove the class from the ObservableCollection (UI)
                    BookedClasses.Remove(booking);

                    // Delete the booking record from the "bookings" node in Firebase
                    var bookingSnapshot = await _firebaseClient
                        .Child("bookings")
                        .OnceAsync<Booking>();

                    // Find and remove the booking that matches the email and class
                    var bookingToDelete = bookingSnapshot.FirstOrDefault(item => item.Object.ClassId == booking.ClassId && item.Object.Email == booking.Email);

                    if (bookingToDelete != null)
                    {
                        // Delete the booking from Firebase
                        await _firebaseClient
                            .Child("bookings")
                            .Child(bookingToDelete.Key)  // Get the booking key and delete it
                            .DeleteAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", "Booking not found in the database.", "OK");
                        return;
                    }

                    // Update the corresponding class in the "classes" node to remove the user's email
                    var firebaseClass = await _firebaseClient
                        .Child("classes")
                        .Child(booking.ClassId)  // Use the ClassId to get the correct class
                        .OnceSingleAsync<YogaClass>();

                    if (firebaseClass != null)
                    {
                        // Remove the email from the BookedUsers list (if it exists)
                        if (firebaseClass.BookedUsers != null && firebaseClass.BookedUsers.Contains(booking.Email))
                        {
                            firebaseClass.BookedUsers.Remove(booking.Email);

                            // Update the class with the new list of booked users
                            await _firebaseClient
                                .Child("classes")
                                .Child(booking.ClassId)
                                .PutAsync(firebaseClass);  // Update the class after email removal
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "Class not found in the database.", "OK");
                        return;
                    }

                    // Sanitize email to make it a valid Firebase path
                    var sanitizedEmail = booking.Email.Replace(".", "_");

                    // Remove the customer's booking from the "customers" node in Firebase
                    var customerRef = _firebaseClient
                        .Child("customers")
                        .Child(sanitizedEmail);  // The sanitized email is used as the key in the "customers" node

                    // Delete the customer's bookings (email) from the "customers" node
                    await customerRef.DeleteAsync();

                    // Reload booked classes to update the UI
                    await LoadBookedClasses();  // Refresh the list and total count
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during the cancel process
                    await DisplayAlert("Error", "An error occurred while canceling the class: " + ex.Message, "OK");
                }
            }
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
    }
}
