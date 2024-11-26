using Microsoft.Maui.Controls;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace YogaCustomerApp.Views
{
    public partial class CartPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;

        public ObservableCollection<YogaClass> CartClasses { get; set; }

        public CartPage(ObservableCollection<YogaClass> cartClasses)
        {
            InitializeComponent();
            _firebaseClient = new FirebaseClient("https://yogaapp-b76cc-default-rtdb.firebaseio.com/");
            CartClasses = cartClasses;
            BindingContext = this;

            // Calculate the total price and update the label
            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            decimal totalPrice = 0;

            // Sum the price of each class in the cart
            foreach (var yogaClass in CartClasses)
            {
                totalPrice += yogaClass.Price;
            }

            // Update the TotalPriceLabel with the total price
            TotalPriceLabel.Text = totalPrice.ToString("C");
        }

        // Handle booking when "Book Now" button is clicked
        private async void OnBookClassClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;  // Get the email entered by the user

            if (string.IsNullOrEmpty(email))
            {
                await DisplayAlert("Error", "Please enter your email address", "OK");
                return;
            }

            // Sanitize email to make it a valid Firebase path key
            var sanitizedEmail = email.Replace(".", "_");

            foreach (var yogaClass in CartClasses)
            {
                var classId = yogaClass.FirebaseId;  // Assuming you have this field as the unique identifier

                // Check if the booking already exists in the "bookings" node
                var existingBookings = await _firebaseClient
                    .Child("bookings")
                    .OnceAsync<Booking>();

                var duplicateBooking = existingBookings.FirstOrDefault(b =>
                    b.Object.ClassId == classId && b.Object.Email == email);

                if (duplicateBooking != null)
                {
                    // Display alert if a duplicate booking is found
                    await DisplayAlert("Already Booked", $"You have already booked the class {yogaClass.ClassType} on {yogaClass.Date}.", "OK");
                    continue; // Skip to the next class
                }

                // Add a new booking record to the "bookings" node
                var booking = new Booking
                {
                    Email = email,
                    ClassId = classId,
                    ClassType = yogaClass.ClassType,
                    Date = yogaClass.Date,
                    Price = yogaClass.Price,
                    Status = "booked",
                    TeacherName = yogaClass.TeacherName
                };

                await _firebaseClient
                    .Child("bookings")
                    .PostAsync(booking);

                // Fetch the class from Firebase
                var firebaseClass = await _firebaseClient
                    .Child("classes")
                    .Child(classId)
                    .OnceSingleAsync<YogaClass>();

                if (firebaseClass != null)
                {
                    // Add email to BookedUsers if not already present
                    if (firebaseClass.BookedUsers == null)
                        firebaseClass.BookedUsers = new List<string>();

                    if (!firebaseClass.BookedUsers.Contains(email))
                    {
                        firebaseClass.BookedUsers.Add(email);

                        // Update the "classes" node with the new list of emails
                        await _firebaseClient
                            .Child("classes")
                            .Child(classId)
                            .Child("BookedUsers")
                            .PutAsync(firebaseClass.BookedUsers);
                    }
                }

                // Update the customer node with the booked class for the email
                var customerBookingRef = _firebaseClient
                    .Child("customers")
                    .Child(sanitizedEmail);

                // Class details to add under the customer's BookedClasses node
                var classDetails = new Dictionary<string, object>
                {
                    { "ClassType", yogaClass.ClassType },
                    { "Price", yogaClass.Price },
                    { "TeacherName", yogaClass.TeacherName },
                    { "Date", yogaClass.Date }
                };

                // Add or update the booking under the customer's BookedClasses node
                await customerBookingRef.Child("BookedClasses").Child(classId).PutAsync(classDetails);

                await DisplayAlert("Success", $"You have successfully booked the {yogaClass.ClassType} class with {yogaClass.TeacherName} on {yogaClass.Date}!", "OK");
            }

            // Optionally, clear the cart after booking
            CartClasses.Clear();
            UpdateTotalPrice();
        }

        // Handle Delete when the delete button is clicked
        private void OnDeleteClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var yogaClass = button.BindingContext as YogaClass;  // Get the current class context

            if (yogaClass != null)
            {
                // Remove class from the ObservableCollection
                CartClasses.Remove(yogaClass);
                UpdateTotalPrice();
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
