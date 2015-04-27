using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Controls.Maps;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace MapGeolocationSample.MapContentControls
{
    [ContentProperty(Name = "Content")]
    public class PushPin : MapContentControl
    {
        public PushPin()
        {
            this.DefaultStyleKey = typeof(PushPin);
        }
    }
}
