using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using MapGeolocationSample.Common;

namespace MapGeolocationSample.Data
{
    public class GeolocationData : INotifyPropertyChanged
    {
        private Geopoint userLocation = new Geopoint(new BasicGeoposition { Latitude = 37.7835370574063, Longitude = -122.403381217767 });
        public Geopoint UserLocation
        {
            get
            {
                return userLocation;
            }
            set
            {
                if (value != userLocation)
                {
                    userLocation = value;
                    NotifyPropertyChanged("UserLocation");
                }
            }
        }

        private bool follow = false;
        public bool Follow
        {
            get
            {
                return follow;
            }
            set
            {
                if (value != follow)
                {
                    follow = value;
                    if (follow)
                    {
                        StartFollow();
                    }
                    else
                    {
                        StopFollow();
                    }
                    NotifyPropertyChanged("Follow");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private void StartFollow()
        {
            try
            {
                if (_geolocator == null)
                {
                    _geolocator = new Geolocator();
                    _geolocator.DesiredAccuracyInMeters = 1;
                    _geolocator.MovementThreshold = 1;
                }

                _geolocator.PositionChanged += geolocator_PositionChanged;
                _geolocator.StatusChanged += geolocator_StatusChanged;

                //this.Center.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // Error as needed. 
                // For this case we just do not populate location instead of showing error - plenty samples out there.
                Debug.WriteLine(ex.ToString());
            }
            //Check when there is a new position
            //var followPosition = GeofenceMonitor.Current.LastKnownGeoposition;

        }

        private Geolocator _geolocator;

        void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {

        }

        private async void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {               
                UserLocation = args.Position.Coordinate.Point; 
            });
                      
        }

        private void StopFollow()
        {
            _geolocator.PositionChanged -= geolocator_PositionChanged;
            _geolocator.StatusChanged -= geolocator_StatusChanged;
        }
    }


    public class GeolocationDataSource
    {
        private static GeolocationDataSource geolocationDataSource = new GeolocationDataSource();

        private ObservableDictionary properties = new ObservableDictionary();
        internal ObservableDictionary Properties
        {
            get { return this.properties; }
        }

        private void SetProperties()
        {
            if (this.properties.Count != 0)
                return;

            properties.Add("UserLocation", new GeolocationData());
        }

        public static object GetProperty(string key)
        {
            geolocationDataSource.SetProperties();
            return geolocationDataSource.Properties[key];
        }


    }

}
