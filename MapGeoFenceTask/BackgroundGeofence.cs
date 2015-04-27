using MapsSampleData.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Notifications;

namespace MapGeoFenceTask
{
    public sealed class BackgroundGeofence : IBackgroundTask
    {
        IBackgroundTaskInstance thisTaskInstance;
        BackgroundTaskDeferral deferral;
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            thisTaskInstance = taskInstance;

            // This is for me to remember to mention that Deferral is needed if there are async operations in the background task
            deferral = taskInstance.GetDeferral();

            string ExceptionData = "None";
            try
            {
                // Get reports for geofences state changes
                foreach (GeofenceStateChangeReport report in GeofenceMonitor.Current.ReadReports())
                {
                    GeofenceState state = report.NewState;
                    Geofence geofence = report.Geofence;

                    var item = await SampleDataSource.GetItemAsync(geofence.Id);


                    // If we've entered a particular geofence, show message for that geofence's id
                    if (state == GeofenceState.Entered)
                    {
                        // Show message for the given geofence enter event
                        string geofenceEnterMessage = "BG Approaching " + item.Subtitle;

                        // Create a toast to tell the user about the event
                        SendToastBG(geofenceEnterMessage);
                        // Or do stuff under the covers, such as update info, change something in the phone, send message, etc
                    }

                    // If we've exited a particular geofence, show message for that geofence's id
                    else if (state == GeofenceState.Exited)
                    {
                        // Show message for the given geofence exit event
                        string geofenceExitMessage = "BG Leaving " + item.Subtitle;
                        // Create a toast to tell the user about the event
                        //SendToastBG(geofenceExitMessage);
                        // Or do stuff under the covers, such as check out, change something in the phone, send message, etc
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionData = ex.Message;
            }

            deferral.Complete();
        }
        public void SendToastBG(string message)
        {
            var toastTemplate =
                ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);

            var text = toastTemplate.GetElementsByTagName("text")[0] as XmlElement;
            text.AppendChild(toastTemplate.CreateTextNode(message));

            var toastNotification = new ToastNotification(toastTemplate);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toastNotification);
        }
    }
}
