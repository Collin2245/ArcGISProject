using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArcGISProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MapViewModel _mapViewModel;
        public MainWindow()
        {
            InitializeComponent();

            _mapViewModel = this.FindResource("MapViewModel") as MapViewModel;
            _mapViewModel.MapView = EsriMapView;

            EsriMapView.GeoViewTapped += EsriMapView_GeoViewTapped;
        }

        private void EsriMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs geoViewInputEvent)
        {
            _mapViewModel.MapClicked(geoViewInputEvent.Location);
        }
    }
}
