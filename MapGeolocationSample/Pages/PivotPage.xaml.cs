using MapGeolocationSample.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Geolocation.Helpers;
using Windows.UI;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using MapsSampleData.DataModel;
using MapGeolocationSample.Data;
using MapGeolocationSample.MapContentControls;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace MapGeolocationSample.Pages
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class PivotPage : Page
    {
        private const string FirstGroupName = "FirstGroup";
        private const string SecondGroupName = "SecondGroup";
        private const string UserLocationName = "UserLocation";

        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

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

        private BasicGeoposition center;
        private double zoomLevel = double.NaN;

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
        /// session. The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("PointsOfInterest-2");
            this.DefaultViewModel[FirstGroupName] = sampleDataGroup;
            this.DefaultViewModel[UserLocationName] = GeolocationDataSource.GetProperty(UserLocationName);

            try
            {
                if (e.PageState.ContainsKey("ZoomLevel"))
                {
                    center = (BasicGeoposition)e.PageState["Center"];
                    zoomLevel = (double)e.PageState["ZoomLevel"];
                }
            }
            catch
            {

            }

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
            e.PageState.Add("Center", this.map.Center.Position);
            e.PageState.Add("ZoomLevel", this.map.ZoomLevel);

        }

        /// <summary>
        /// Invoked when an item within a section is clicked.
        /// </summary>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((SampleDataItem)e.ClickedItem).UniqueId;
            if (!Frame.Navigate(typeof(ItemPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private Dictionary<string, MapElement> gasStationFenceDictionary = new Dictionary<string, MapElement>();


        #region Demo 3B: MapIcons
        /// <summary>
        /// Loads the content for the second pivot item when it is scrolled into view. Second pivot is named "cars"
        /// </summary>
        private async void SecondPivot_Loaded(object sender, RoutedEventArgs e)
        {
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("PointsOfInterest-1");
            this.DefaultViewModel[SecondGroupName] = sampleDataGroup;

            initFences();

            foreach (SampleDataItem gasStationItem in ((SampleDataGroup)DefaultViewModel[FirstGroupName]).Items)
            {
                MapIcon gasStationIcon = new MapIcon();
                gasStationIcon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/map.categories.petrolstation.tier20.png"));
                gasStationIcon.NormalizedAnchorPoint = new Point(0.5, 0.5);
                gasStationIcon.Location = gasStationItem.Location;
                gasStationIcon.Title = gasStationItem.Title;
                gasStationIcon.CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible;
                this.map.MapElements.Add(gasStationIcon);

                Geocircle gasStationFence = new Geocircle(gasStationItem.Location.Position, 250.0);

                addFence(gasStationItem.UniqueId, gasStationFence);
                updateGasStationFence(gasStationItem.UniqueId, gasStationFence, Windows.UI.Colors.Blue);
            }

            if (!double.IsNaN(zoomLevel))
            {
                await this.map.TrySetViewAsync(new Geopoint(center), zoomLevel);
            }
            else
            {
                await this.map.TrySetViewBoundsAsync(sampleDataGroup.BestMapViewBoxList, null, Windows.UI.Xaml.Controls.Maps.MapAnimationKind.None);
            }

        }
        #endregion

        #region Demo 3C: MapPolygon
        private void updateGasStationFence(string uniqueId, Geocircle gasStationFence, Color fillColor)
        {
            if (gasStationFenceDictionary.ContainsKey(uniqueId))
            {
                this.map.MapElements.Remove(gasStationFenceDictionary[uniqueId]);
                gasStationFenceDictionary.Remove(uniqueId);
            }

            MapPolygon gasStationPolygon = new MapPolygon();
            gasStationPolygon.Path = gasStationFence.ToGeopath();
            fillColor.A = 50;
            gasStationPolygon.FillColor = fillColor;
            gasStationPolygon.StrokeThickness = 0;

            gasStationFenceDictionary.Add(uniqueId, gasStationPolygon);
            this.map.MapElements.Add(gasStationPolygon);
        }
        #endregion

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

        private void PushPin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((string)((PushPin)sender).Tag);
            if (!Frame.Navigate(typeof(DirectionsPage), itemId))
            {
                throw new Exception(this.resourceLoader.GetString("NavigationFailedExceptionMessage"));
            }
        }

        private async void ZoomInAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                List<BasicGeoposition> points = new List<BasicGeoposition>();
                foreach (SampleDataItem rscItem in ((SampleDataGroup)DefaultViewModel[SecondGroupName]).Items)
                {
                    points.Add(rscItem.Location.Position);
                }

                await this.map.TrySetViewBoundsAsync(GeoboundingBox.TryCompute(points), null, MapAnimationKind.Bow);
            }
        }

        #region Demo 3D: Custion Map Tiles
        private bool showRegion = false;
        private void RegionAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                if (!showRegion)
                {
                    this.map.TileSources.Add(new InvertMaskMapTileSource(((SampleDataGroup)this.DefaultViewModel[SecondGroupName]).ShapePath, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(204, 204, 204, 204)));
                    showRegion = true;
                }
                else
                {
                    this.map.TileSources.Clear();
                    showRegion = false;
                }
            }
        }
        #endregion

        #region Demo 3E: Traffic Property
        private bool showTraffic = false;
        private void TrafficAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                if (!showTraffic)
                {
                    this.map.TrafficFlowVisible = true;
                    showTraffic = true;
                }
                else
                {
                    this.map.TrafficFlowVisible = false;
                    showTraffic = false;
                }
            }
        }
        #endregion

        #region Follow Geolocator
        private bool following = false;
        private void FollowAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                if (!following)
                {
                    ((GeolocationData)this.DefaultViewModel[UserLocationName]).Follow = true;
                    ((GeolocationData)this.DefaultViewModel[UserLocationName]).PropertyChanged += PivotPage_PropertyChanged;
                    following = true;
                }
                else
                {
                    ((GeolocationData)this.DefaultViewModel[UserLocationName]).Follow = false;
                    following = false;
                }
            }
        }

        async void PivotPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UserLocation")
            {
                await this.map.TrySetViewAsync(((GeolocationData)sender).UserLocation, 15.5, 0, 0, MapAnimationKind.Linear);
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                this.map.ZoomLevel -= 1;
            }
        }
        #endregion

        #region Demo 4: Geofencing

        private IBackgroundTaskRegistration geofenceTask = null;
        private const string backgroundTaskName = "BackgroundGeofence";
        private const string backgroundTaskEntryPoint = "MapDemoGeofenceTask.BackgroundGeofence";
        private bool backgroundFences = false;

        private void initFences()
        {
            if (!backgroundFences)
            {
                ///// FOREGROUND CASE
                ////STEP 1: Register for GeofenceMonitor status change events
                GeofenceMonitor.Current.StatusChanged += OnStatusChangedHandler;

                ////STEP 2: Register for geofence state change events
                GeofenceMonitor.Current.GeofenceStateChanged += OnGeofenceStateChangedHandler;
            }
            else
            {
                GeofenceMonitor.Current.StatusChanged -= OnStatusChangedHandler;
                GeofenceMonitor.Current.GeofenceStateChanged -= OnGeofenceStateChangedHandler;
                /// BACKGROUND CASE
                //STEP 3: Initialize geofence background task
                InitializeGeofenceBackgroundTask();
            }
            GeofenceMonitor.Current.Geofences.Clear();
        }

        /// <summary>
        /// Invoked when the user clicks on the UI button to add a geofence.
        /// </summary>
        /// <param name="sender"></param>        
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        private void addFence(string uniqueId, Geocircle geoCircle)
        {
            try
            {
                string geofenceId = uniqueId;
                //STEP 4: Create the geofences using constructors

                //Basic constructor. Monitored states will be both Entered and Exited.
                geofenceId = uniqueId + "WithDefaults";
                Geofence geofenceBasic = new Geofence(geofenceId, geoCircle);

                //Constructor to indicate single use or permanent 
                geofenceId = uniqueId + "WithSingleUse";
                Boolean useOnce = true;
                Geofence geofenceSingleUse = new Geofence(geofenceId, geoCircle, MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Removed, useOnce);

                //Constructor to indicate no-single use and dwell time options
                geofenceId = uniqueId;
                useOnce = false;
                TimeSpan stayDuration = new TimeSpan();
                stayDuration = TimeSpan.FromSeconds(1);
                Geofence geofenceWithDuration = new Geofence(geofenceId, geoCircle, MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited, useOnce, stayDuration);

                //Constructor to indicate no-single use, dwell time and validity period options
                geofenceId = uniqueId + "WithValidityPeriodAndDwellTime3s";
                useOnce = false;
                stayDuration = TimeSpan.FromSeconds(3);
                DateTime today = DateTime.Now;
                DateTimeOffset validFrom = today;
                TimeSpan validFor = new TimeSpan(0, 2, 0);
                Geofence geofenceWithValidityPeriod = new Geofence(geofenceId, geoCircle, MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited, useOnce, stayDuration, validFrom, validFor);



                // STEP 5: Add the geofences to the GeofenceMonitor

                //GeofenceMonitor.Current.Geofences.Add(geofenceBasic);
                //GeofenceMonitor.Current.Geofences.Add(geofenceSingleUse);
                GeofenceMonitor.Current.Geofences.Add(geofenceWithDuration);
                //GeofenceMonitor.Current.Geofences.Add(geofenceWithValidityPeriod);

            }

            catch (UnauthorizedAccessException)
            {
                // The user has not granted Location permission to this application
                // Throw an error and pop the user to enable Location
                Debug.WriteLine("Error: Location is not enabled or not allowed for this app. Please check the location status under: settings -> location");
            }
            catch (Exception ex)
            {
                // GeofenceMonitor failed in adding a geofence
                // exceptions could be from out of memory, lat/long out of range,
                // too long a name, not a unique name
                // Error will need to be handled depending on the case
                Debug.WriteLine("Error:" + ex.ToString());
            }

        }

        /// STEP 1B: On geofence monitor status change, read status and proceed accordingly
        /// Event handler for geofence monitor status changes.
        /// </summary>
        /// <param name="sender">GeofenceMonitor, which receives the event notifications.</param>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        public async void OnStatusChangedHandler(GeofenceMonitor sender, object e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var status = sender.Status;

                string eventDescription = "Geofence Status Changed: ";

                if (GeofenceMonitorStatus.Ready == status)
                {
                    Debug.WriteLine(eventDescription + " (Ready)");
                }
                else if (GeofenceMonitorStatus.Initializing == status)
                {
                    Debug.WriteLine(eventDescription + " (Initializing)");

                }
                else if (GeofenceMonitorStatus.NoData == status)
                {
                    Debug.WriteLine(eventDescription + " (NoData)");

                }
                else if (GeofenceMonitorStatus.Disabled == status)
                {
                    Debug.WriteLine(eventDescription + " (Disabled)");

                }
                else if (GeofenceMonitorStatus.NotInitialized == status)
                {
                    Debug.WriteLine(eventDescription + " (NotInitialized)");

                }
                else if (GeofenceMonitorStatus.NotAvailable == status)
                {
                    Debug.WriteLine(eventDescription + " (NotAvailable)");

                }
            });
        }

        /// STEP 2B: On geofence notification, read reports
        /// Event handler for geofence state changes.
        /// </summary>
        /// <param name="sender">GeofenceMonitor, which receives the event notifications.</param>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        public async void OnGeofenceStateChangedHandler(GeofenceMonitor sender, object e)
        {
            // Get reports for geofences state changes
            var reports = sender.ReadReports();

            foreach (GeofenceStateChangeReport report in reports)
            {
                GeofenceState state = report.NewState;
                Windows.Devices.Geolocation.Geofencing.Geofence geofence = report.Geofence;
                var gasStationItem = await SampleDataSource.GetItemAsync(geofence.Id);

                // If we've entered a particular geofence, show message for that geofence's id
                if (state == GeofenceState.Entered)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Show message for the given geofence enter event
                        string geofenceEnterMessage = "Approaching " + gasStationItem.Subtitle;
                        // Post message update
                        Debug.WriteLine(geofenceEnterMessage);
                        // Update Fence
                        updateGasStationFence(report.Geofence.Id, (Geocircle)report.Geofence.Geoshape, Colors.Green);
                        // Or create a toast to tell the user about the event
                        SendToast(geofenceEnterMessage);
                        // Or do stuff under the covers, such as update info, change something in the phone, send message, etc
                    });

                }

                // If we've exited a particular geofence, show message for that geofence's id
                else if (state == GeofenceState.Exited)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        // Show message for the given geofence exit event
                        string geofenceExitMessage = "Departing " + gasStationItem.Subtitle;
                        // Post message update
                        Debug.WriteLine(geofenceExitMessage);
                        // Draw Fence
                        updateGasStationFence(report.Geofence.Id, (Geocircle)report.Geofence.Geoshape, Colors.Red);
                        // Or create a toast to tell the user about the event
                        //SendToast(geofenceExitMessage);
                        // Or do stuff under the covers, such as check out, change something in the phone, send message, etc
                    });
                }
                else if (state == GeofenceState.Removed)
                {
                    GeofenceRemovalReason reason = report.RemovalReason;
                    string eventDescription = "Geofence Removed: ";

                    if (reason == GeofenceRemovalReason.Expired)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            eventDescription += " (Removed/Expired)";
                            Debug.WriteLine(eventDescription + geofence.Id);
                        });
                    }
                    else if (reason == GeofenceRemovalReason.Used)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            eventDescription += " (Removed/Used)";
                            Debug.WriteLine(eventDescription + geofence.Id);
                        });
                    }
                }
            }
        }

        /// <summary>
        ///  Present a toast on geofence enter/exit event
        /// </summary>
        /// <param name="geofenceEnterMessage"></param>
        private void SendToast(string message)
        {
            var toastTemplate =
                ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

            var text = toastTemplate.GetElementsByTagName("text")[0] as XmlElement;
            text.AppendChild(toastTemplate.CreateTextNode(message));

            var toastNotification = new ToastNotification(toastTemplate);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toastNotification);
        }

        #endregion

        #region Demo 5: Backgrond Tasks

        private void BackgroundGeofenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.pivot.SelectedIndex == 1)
            {
                backgroundFences = true;
                initFences();
            }
        }

        /// STEP 5B: Initialize the background task
        /// <summary>
        /// If geofence background task is already registered, checks its status. Otherwise
        /// registers the geofence background task.
        /// </summary>
        private void InitializeGeofenceBackgroundTask()
        {
            // Check if the task is already registered
            // Enumerate over all existing background tasks registered by this app
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == backgroundTaskName)
                {
                    geofenceTask = cur.Value;
                    break;
                }
            }

            if (geofenceTask != null)
            {
                // Optional: Associate a foreground event handler with the existing background task
                // Needed if the background task needs to communicate to the main app, and the main app handles the events
                //geofenceTask.Completed += new BackgroundTaskCompletedEventHandler(OnGeofenceBackgroundTaskCompleted);

                BackgroundAccessStatus backgroundAccessStatus = BackgroundExecutionManager.GetAccessStatus();

                switch (backgroundAccessStatus)
                {
                    case BackgroundAccessStatus.Unspecified:
                    case BackgroundAccessStatus.Denied:
                        Debug.WriteLine("Background execution is disabled for this app.");
                        break;
                    default:
                        Debug.WriteLine("Background task is already registered. Waiting for next update...");
                        break;
                }

            }
            else
            {
                // Background task not already registered
                RegisterGeofenceBackgroundTask();
            }
        }

        async private void RegisterGeofenceBackgroundTask()
        {
            // Request access for background task. No prompt is shown to the user in Windows Phone.
            BackgroundAccessStatus backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();

            // Create a new background task builder
            BackgroundTaskBuilder geofenceTaskBuilder = new BackgroundTaskBuilder();

            geofenceTaskBuilder.Name = backgroundTaskName;
            geofenceTaskBuilder.TaskEntryPoint = backgroundTaskEntryPoint;

            // Create a new location trigger
            var trigger = new LocationTrigger(LocationTriggerType.Geofence);

            // Associate the location trigger with the background task builder
            geofenceTaskBuilder.SetTrigger(trigger);

            // Register the background task
            geofenceTask = geofenceTaskBuilder.Register();

            // Optional Associate an event handler with the new background task
            // geofenceTask.Completed += new BackgroundTaskCompletedEventHandler(OnGeofenceBackgroundTaskCompleted);

            switch (backgroundAccessStatus)
            {
                case BackgroundAccessStatus.Unspecified:
                case BackgroundAccessStatus.Denied:
                    Debug.WriteLine("This application must be added to the lock screen before the background task will run.");
                    break;

                default:
                    // The following would need to be done in Windows, but not needed in Windows Phone 
                    // Ensure we have presented the location consent prompt (by asynchronously getting the current
                    // position). This must be done here because the background task cannot display UI.
                    //GetGeopositionAsync();
                    break;
            }
        }
        /// <summary>
        /// Optional: Callback for geofence background task completion.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGeofenceBackgroundTaskCompleted(IBackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs e)
        {
            Debug.WriteLine("Background task completed.");
            if (sender != null)
            {
                // Update the UI with progress reported by the background task
                try
                {
                    e.CheckResult();
                }
                catch (Exception ex)
                {
                    // The background task had an error
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
        #endregion

    }
}
