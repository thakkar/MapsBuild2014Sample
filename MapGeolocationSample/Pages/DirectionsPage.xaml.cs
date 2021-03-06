﻿using MapGeolocationSample.Common;
using MapGeolocationSample.Data;
using MapsSampleData.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace MapGeolocationSample.Pages
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class DirectionsPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private const string UserLocationName = "UserLocation";

        public DirectionsPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data.
            var item = await SampleDataSource.GetItemAsync((string)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;
            this.DefaultViewModel[UserLocationName] = GeolocationDataSource.GetProperty(UserLocationName);
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.           
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button selected = sender as Button;
            string uriString = null;
            SampleDataItem item = DefaultViewModel["Item"] as SampleDataItem;
            if (selected != null && item != null)
            {
                string lat = item.Location.Position.Latitude.ToString();
                string lng = item.Location.Position.Longitude.ToString();
                string name = Uri.EscapeDataString(item.Subtitle);

                switch (selected.Name)
                {
                    case "Button4":  // Start Voice Nav
                        uriString = "ms-drive-to:?destination.latitude=" + lat + "&destination.longitude=" + lng + "&destination.name=" + name;
                        break;
                    default:
                        break;
                }
            }
            if (uriString != null)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(uriString));
            }
        }

        private ObservableCollection<SampleDataGroup> _groups = new ObservableCollection<SampleDataGroup>();
        public ObservableCollection<SampleDataGroup> Groups
        {
            get { return this._groups; }
        }

        #region Demo 2: Getting Directions
        private async void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            MapControl map = (MapControl)sender;
            SampleDataItem item = (SampleDataItem)DefaultViewModel["Item"];
            GeolocationData userLocation = (GeolocationData)DefaultViewModel[UserLocationName];

            Geopoint start = userLocation.UserLocation;
            Geopoint end = item.Location;
            MapRouteFinderResult routeResult = await MapRouteFinder.GetWalkingRouteAsync(start, end);
            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                MapRoute route = routeResult.Route;
                MapRouteView routeView = new MapRouteView(route);
                map.Routes.Add(routeView);
                var maneuvers = route.Legs[0].Maneuvers;
                maneuverList.ItemsSource = maneuvers;
                await map.TrySetViewBoundsAsync(route.BoundingBox, new Thickness(15, 30, 15, 30), MapAnimationKind.None);
            }
        }
        #endregion
    }
}
