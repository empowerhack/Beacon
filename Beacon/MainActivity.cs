using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Telephony;
using Android.Gms.Common.Apis;
using Android.Gms.Location;
using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Locations;
using Java.Util;
using Android.Views;
using Android.Content;

namespace Beacon
{
    [Activity(Label = "Beacon", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        GoogleApiClient apiClient;
        bool connected = false;
        string phoneNumber = "";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            LinearLayout button = FindViewById<LinearLayout>(Resource.Id.MyButton);
            button.Click += Button_Click;

            Button changeNumBtn = FindViewById<Button>(Resource.Id.ChangeNumber);
            changeNumBtn.Click += ChangeNumBtn_Click;

            Button infoBtn = FindViewById<Button>(Resource.Id.Info);
            infoBtn.Click += InfoBtn_Click;

            apiClient = new GoogleApiClient.Builder(this)
             .AddApi(LocationServices.API)
             .AddConnectionCallbacks(OnConnect, OnSuspended)
             .AddOnConnectionFailedListener(OnFailed)
             .Build();
        }

        private void InfoBtn_Click(object sender, EventArgs e)
        {
            View view = LayoutInflater.Inflate(Resource.Layout.scrollalert, null);

            TextView textview = (TextView)view.FindViewById(Resource.Id.textmsg);
            textview.Text = "Familyr aims to help separated refugee families reunite with their loved ones and be re-assured that someone knows where they are.\n" +
                "Many refugee families only possess one mobile phone, which the husband is usually in control of. During the confusion of crossing borders and other chaotic " +
                "situations they can be separated, with no way of contacting each other. Familyr would supply each family member with a ultra low-budget device, which " +
                "- at the push a button - notifies the family phone with the member's location.\nThis is a simple prototype to show the potential of the Familyr project.";

            AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
            alertDialog.SetTitle("About Familyr");
            alertDialog.SetView(view);
            alertDialog.SetPositiveButton("Sweet!", (afk, kfa) => { });
            AlertDialog alert = alertDialog.Create();
            alert.Show();
        }

        private void ChangeNumBtn_Click(object sender, EventArgs e)
        {
            ChangePhoneNum(false);
        }

        protected override void OnStart()
        {
            base.OnStart();
            apiClient.Connect();
        }

        protected override void OnStop()
        {
            connected = false;
            apiClient.Disconnect();
            base.OnStop();
        }

        private void OnConnect(Bundle bundle)
        {
            connected = true;
            Toast.MakeText(this, "Connected!", ToastLength.Short).Show();
        }

        private void OnSuspended(int num)
        {
            
        }

        private void ChangePhoneNum(bool send)
        {
            // Set up the input
            EditText input = new EditText(this);
            input.InputType = Android.Text.InputTypes.ClassPhone;
            input.Text = phoneNumber;

            AlertDialog inputDialog = new AlertDialog.Builder(this)
                .SetTitle("Set Phone Number")
                .SetMessage("Which phone number should the message be sent to?")
                .SetView(input)
                .SetPositiveButton("Set", (afk, kfa) => {
                    phoneNumber = input.Text;
                    if (send) SendMessage();
                })
                .SetNegativeButton("Cancel", (afk, kfa) => { })
                .Create();

            inputDialog.Show();
        }

        private void OnFailed(Android.Gms.Common.ConnectionResult result)
        {
            Toast.MakeText(this, "Failed to connect to google places API", ToastLength.Long).Show();
        }

        private async void SendMessage()
        {
            if(string.IsNullOrWhiteSpace(phoneNumber))
            {
                ChangePhoneNum(true);
                return;
            }

            TelephonyManager manager = (TelephonyManager)this.GetSystemService(Context.TelephonyService);
            if (manager.PhoneType == PhoneType.None)
            {
                // No SMS capability!
                AlertDialog alert = new AlertDialog.Builder(this)
                    .SetTitle("Error!")
                    .SetMessage("You can't send text messages on this device! Familyr works by sending SMS messages.")
                    .Create();
                alert.Show();

                return;
            }

            if (!connected)
            {
                Toast.MakeText(this, "Waiting for connection to API", ToastLength.Long).Show();
                while (!connected)
                {
                    await Task.Delay(1000);
                }
            }

            try
            {
                Location lastLoc = LocationServices.FusedLocationApi.GetLastLocation(apiClient);

                if (lastLoc != null)
                {
                    double lat = lastLoc.Latitude;
                    double lon = lastLoc.Longitude;
                    Geocoder geocoder = new Geocoder(this, Locale.Default);
                    IList<Address> addresses = await geocoder.GetFromLocationAsync(lat, lon, 1);

                    if (addresses == null || addresses.Count == 0)
                    {
                        Toast.MakeText(this, "No addresses found", ToastLength.Long).Show();
                        return;
                    }
                    Address add = addresses[0];

                    string message = string.Format("{0} has reported their location at {1}, {2}.\n" +
                        "Lat: {3}\n" +
                        "Long: {4}\n" +
                        "http://maps.google.com/maps?q=loc:{3},{4}",
                        "Familyr member 1", add.Locality, add.PostalCode, lat, lon);

                    SmsManager.Default.SendTextMessage(phoneNumber, null, message, null, null);

                    Toast.MakeText(this, "Sent!", ToastLength.Long).Show();
                }
                else
                {
                    Toast.MakeText(this, "Failed to get location", ToastLength.Long).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            SendMessage();
        }
    }
}

