using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace MapGeolocationSample.MapContentControls
{
    /// <summary>
    /// Represents a child of a map, which uses geographic coordinates to position itself.
    /// </summary>
    [ContentProperty(Name = "Content")]
    public class MapContentControl : ContentControl
    {

        #region Dependency properties

        /// <summary>
        /// Identifies the <see cref="GeoCoordinate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(
            "Location",
            typeof(Geopoint),
            typeof(MapContentControl),
            new PropertyMetadata(null, OnLocationChangedCallback));

        /// <summary>
        /// Identifies the <see cref="PositionOrigin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NormalizedAnchorPointProperty = DependencyProperty.Register(
            "NormalizedAnchorPoint",
            typeof(Point),
            typeof(MapContentControl),
            new PropertyMetadata(null, OnNormalizedAnchorPointChangedCallback));

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the geographic coordinate of the control on the map.
        /// </summary>
        // [TypeConverter(typeof(GeoCoordinateConverter))]
        public Geopoint Location
        {
            get { return (Geopoint)this.GetValue(LocationProperty); }
            set { this.SetValue(LocationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the position origin of the control, which defines the position on the control to anchor to the map.
        /// </summary>
        public Point NormalizedAnchorPoint
        {
            get { return (Point)this.GetValue(NormalizedAnchorPointProperty); }
            set { this.SetValue(NormalizedAnchorPointProperty, value); }
        }

        #endregion

        #region Private static handlers for the dependency properties

        /// <summary>
        /// Callback method on object when GeoCoordinate is changed. 
        /// </summary>
        /// <param name="d">dependency object</param>
        /// <param name="e">event args</param>
        private static void OnLocationChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(MapControl.LocationProperty, e.NewValue);
        }

        /// <summary>
        /// Callback method on object when PositionOrigin is changed. 
        /// </summary>
        /// <param name="d">dependency object</param>
        /// <param name="e">event args</param>
        private static void OnNormalizedAnchorPointChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(MapControl.NormalizedAnchorPointProperty, e.NewValue);
        }

        #endregion
    }
}
