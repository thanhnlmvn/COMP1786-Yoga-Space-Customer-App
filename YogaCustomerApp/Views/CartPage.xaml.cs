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

            // Iterate through the cart to book each class
            foreach (var yogaClass in CartClasses)
            {
                var classId = yogaClass.FirebaseId;  // Assuming you have this field as the unique identifier

                // Fetch the class from Firebase
                var firebaseClass = await _firebaseClient
                    .Child("classes")
                    .Child(classId)  // Using the class ID to get the correct class
                    .OnceSingleAsync<YogaClass>();

                if (firebaseClass != null)
                {
                    // Check if the email is already in the "BookedUsers" list
                    if (firebaseClass.BookedUsers != null && firebaseClass.BookedUsers.Contains(email))
                    {
                        // Display alert if the email is already booked for the class
                        await DisplayAlert("Already Booked", "This email has already booked this class.", "OK");
                        // Keep the class in the cart, do not remove it
                        continue; // Continue to the next class in the cart without removing the current class
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
                        TeacherName = yogaClass.TeacherName  // Set the TeacherName from the class
                    };

                    // Add booking to the "bookings" node
                    await _firebaseClient
                        .Child("bookings")
                        .PostAsync(booking);

                    // Update the emails list directly in the "classes" node
                    if (firebaseClass.BookedUsers == null)
                        firebaseClass.BookedUsers = new List<string>();

                    // Add the email to the "BookedUsers" list
                    firebaseClass.BookedUsers.Add(email);

                    // Update the "classes" node with the new list of emails
                    await _firebaseClient
                        .Child("classes")
                        .Child(classId)
                        .Child("BookedUsers") // Target the "BookedUsers" field for update
                        .PutAsync(firebaseClass.BookedUsers);  // Only update the BookedUsers field

                    // Update the customer node with the booked class for the email
                    var customerBookingRef = _firebaseClient
                        .Child("customers")
                        .Child(sanitizedEmail);  // The email will be the key

                    // Fetch the current bookings for the email
                    var currentBookedClasses = await customerBookingRef.Child("BookedClasses").OnceSingleAsync<Dictionary<string, object>>();

                    // Class details to add under the customer's BookedClasses node
                    var classDetails = new Dictionary<string, object>
                    {
                        { "ClassType", yogaClass.ClassType },
                        { "Price", yogaClass.Price },
                        { "TeacherName", yogaClass.TeacherName },
                        { "Date", yogaClass.Date }
                    };

                    if (currentBookedClasses == null)
                    {
                        // If no bookings exist for this email, create a new BookedClasses node with classId as key
                        await customerBookingRef.Child("BookedClasses").Child(classId).PutAsync(classDetails);
                    }
                    else
                    {
                        // If bookings exist, update the specific classId entry
                        if (!currentBookedClasses.ContainsKey(classId))
                        {
                            await customerBookingRef.Child("BookedClasses").Child(classId).PutAsync(classDetails);
                        }
                    }

                    await DisplayAlert("Success", $"You have successfully booked the {yogaClass.ClassType} class with {yogaClass.TeacherName} on {yogaClass.Date}!", "OK");
                }
            }

            // Optionally, clear the cart after booking (you can comment this if you want to keep the cart)
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
