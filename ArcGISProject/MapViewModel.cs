using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Location;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Esri.ArcGISRuntime.UI;

namespace ArcGISProject
{
    /// <summary>
    /// Provides map data to an application
    /// </summary>
    ///     private Map _map;

    public class MapViewModel : INotifyPropertyChanged
    {
        private MapView _mapView;
        private GraphicsOverlay _graphicsOverlay;
        private MapPoint _startPoint;
        private MapPoint _endPoint;
        private String ServerUrl = "https://www.arcgis.com/sharing/rest";
        private String RouteServiceURI = "https://route.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World";
        private String ClientId = "n5yRQ5tvMCEbqPOC";
        private String RedirectURI = "my-app://auth";
        public MapViewModel()
        {
            //creates map
            CreateNewMap();
            //set auth
            SetOAuthInfo();

        }

        private Map _map = new Map(Basemap.CreateStreetsVector());

        /// <summary>
        /// Gets or sets the map
        /// </summary>
        public Map Map
        {
            get => _map;
            set { _map = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Raises the <see cref="MapViewModel.PropertyChanged" /> event
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;

        private async void CreateNewMap()
        {
            //Image to use
            const string coffeeImage = "https://pngimg.com/uploads/mug_coffee/mug_coffee_PNG16855.png";

            //Dataset to use
            const string coffeeData = "https://services3.arcgis.com/GVgbJbqm8hXASVYi/ArcGIS/rest/services/LA_West_Coffee_Shops/FeatureServer/0";

            //renderer - this adds a renderer that changes the points to the selected image
            SimpleRenderer CoffeeRenderer = addImageToPoint(coffeeImage);

            //feature layer
            Map newMap = new Map(Basemap.CreateImageryWithLabels());
            FeatureLayer coffeeLayer = new FeatureLayer(new Uri(coffeeData));
            await coffeeLayer.LoadAsync();

            //Apply renderer to layer
            coffeeLayer.Renderer = CoffeeRenderer;


            newMap.OperationalLayers.Add(coffeeLayer);
            newMap.InitialViewpoint = new Viewpoint(coffeeLayer.FullExtent);
            Map = newMap;
        }

        private SimpleRenderer addImageToPoint(string linkToPng)
        {
            var imageToUse = new Uri(linkToPng);
            PictureMarkerSymbol symbolToUse = new PictureMarkerSymbol(imageToUse);
            symbolToUse.Width = 30;
            symbolToUse.Height = 30;
            SimpleRenderer SymbolRenderer = new SimpleRenderer(symbolToUse);
            return SymbolRenderer;
        }

        //create auth
        public async Task<Credential> CreateCredentialAsync(CredentialRequestInfo credentialRequestInfo)
        {
            Credential credential = null;
            try
            {
                credential = await AuthenticationManager.Current.GenerateCredentialAsync(credentialRequestInfo.ServiceUri);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return credential;
        }

        //set auth
        private void SetOAuthInfo()
        {
            var serverInfo = new ServerInfo(new Uri(ServerUrl))
            {
                ServerUri = new Uri(ServerUrl),
                TokenAuthenticationType = TokenAuthenticationType.OAuthImplicit,
                OAuthClientInfo = new OAuthClientInfo
                {
                    ClientId = ClientId,
                    RedirectUri = new Uri(RedirectURI)
                }
            };
            AuthenticationManager.Current.RegisterServer(serverInfo);
            AuthenticationManager.Current.OAuthAuthorizeHandler = new OAuthAuthorize();
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(CreateCredentialAsync);
        }

        public MapView MapView
        {
            set { _mapView = value; }
        }

        private void SetGraphicsOverlay()
        {
            if (_mapView != null && _graphicsOverlay == null)
            {
                _graphicsOverlay = new GraphicsOverlay();
                _mapView.GraphicsOverlays.Add(_graphicsOverlay);
            }
        }

        private void SetMapMarker(MapPoint location, SimpleMarkerSymbolStyle pointStyle, System.Drawing.Color markerColor, System.Drawing.Color markerOutlineColor)
        {
            float markerSize = 8.0f;
            float markerOutlineThickness = 2.0f;
            SimpleMarkerSymbol pointSymbol = new SimpleMarkerSymbol(pointStyle, markerColor, markerSize);
            pointSymbol.Outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, markerOutlineColor, markerOutlineThickness);
            Graphic pointGraphic = new Graphic(location, pointSymbol);
            _graphicsOverlay.Graphics.Add(pointGraphic);
        }

        private void SetStartMarker(MapPoint location)
        {
            SetGraphicsOverlay();
            _graphicsOverlay.Graphics.Clear();
            SetMapMarker(location, SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(226, 119, 40), System.Drawing.Color.FromArgb(0, 226, 0));
            _startPoint = location;
            _endPoint = null;
        }

        private void SetEndMarker(MapPoint location)
        {
            SetMapMarker(location, SimpleMarkerSymbolStyle.Square, System.Drawing.Color.FromArgb(40, 119, 226), System.Drawing.Color.FromArgb(226, 0, 0));
            _endPoint = location;
            FindRoute();
        }

        public void MapClicked(MapPoint location)
        {
            if (_startPoint == null)
            {
                SetStartMarker(location);
            }
            else if (_endPoint == null)
            {
                SetEndMarker(location);
            }
            else
            {
                SetStartMarker(location);
            }
        }

        private async void FindRoute()
        {
            try
            {
                RouteTask solveRouteTask = await RouteTask.CreateAsync(new Uri(RouteServiceURI));
                RouteParameters routeParameters = await solveRouteTask.CreateDefaultParametersAsync();
                List<Stop> stops = new List<Stop> { new Stop(_startPoint), new Stop(_endPoint) };
                routeParameters.SetStops(stops);
                RouteResult solveRouteResult = await solveRouteTask.SolveRouteAsync(routeParameters);
                Route firstRoute = solveRouteResult.Routes.FirstOrDefault();
                Polyline routePolyline = firstRoute.RouteGeometry;
                SimpleLineSymbol routeSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.GreenYellow, 4.0f);
                Graphic routeGraphic = new Graphic(routePolyline, routeSymbol);
                _graphicsOverlay.Graphics.Add(routeGraphic);
            }
            catch (Exception exception)
            {
                throw (exception);
            }
        }
    }


        //authorization set up
        public class OAuthAuthorize : IOAuthAuthorizeHandler
        {
            private Window _window;
            private TaskCompletionSource<IDictionary<string, string>> _tcs;
            private string _callbackUrl;
            private string _authorizeUrl;

            public Task<IDictionary<string, string>> AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
            {
                if (_tcs != null || _window != null)
                {
                    throw new Exception();
                }
                _authorizeUrl = authorizeUri.AbsoluteUri;
                _callbackUrl = callbackUri.AbsoluteUri;
                _tcs = new TaskCompletionSource<IDictionary<string, string>>();
                var dispatcher = Application.Current.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                {
                    AuthorizeOnUIThread(_authorizeUrl);
                }
                else
                {
                    dispatcher.BeginInvoke((Action)(() => AuthorizeOnUIThread(_authorizeUrl)));
                }
                return _tcs.Task;
            }

            private void AuthorizeOnUIThread(string authorizeUri)
            {
                var webBrowser = new WebBrowser();
                webBrowser.Navigating += WebBrowserOnNavigating;
                _window = new Window
                {
                    Content = webBrowser,
                    Height = 600,
                    Width = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current != null && Application.Current.MainWindow != null
                                ? Application.Current.MainWindow
                                : null
                };
                _window.Closed += OnWindowClosed;
                webBrowser.Navigate(authorizeUri);
                if (_window != null)
                {
                    _window.ShowDialog();
                }
            }

            void OnWindowClosed(object sender, EventArgs e)
            {
                if (_window != null && _window.Owner != null)
                {
                    _window.Owner.Focus();
                }
                if (_tcs != null && !_tcs.Task.IsCompleted)
                {
                    // The user closed the window
                    _tcs.SetException(new OperationCanceledException());
                }
                _tcs = null;
                _window = null;
            }

            void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs navigationEvent)
            {
                var webBrowser = sender as WebBrowser;
                Uri uri = navigationEvent.Uri;
                if (webBrowser == null || uri == null || _tcs == null || string.IsNullOrEmpty(uri.AbsoluteUri))
                {
                    return;
                }
                if (uri.AbsoluteUri.StartsWith(_callbackUrl))
                {
                    navigationEvent.Cancel = true;
                    var tcs = _tcs;
                    _tcs = null;
                    if (_window != null)
                    {
                        _window.Close();
                    }
                    tcs.SetResult(DecodeParameters(uri));
                }
            }

            private static IDictionary<string, string> DecodeParameters(Uri uri)
            {
                var answer = string.Empty;
                if (!string.IsNullOrEmpty(uri.Fragment))
                {
                    answer = uri.Fragment.Substring(1);
                }
                else if (!string.IsNullOrEmpty(uri.Query))
                {
                    answer = uri.Query.Substring(1);
                }
                var keyValueDictionary = new Dictionary<string, string>();
                var keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var kvString in keysAndValues)
                {
                    var pair = kvString.Split('=');
                    string key = pair[0];
                    string value = string.Empty;
                    if (key.Length > 1)
                    {
                        value = Uri.UnescapeDataString(pair[1]);
                    }
                    keyValueDictionary.Add(key, value);
                }
                return keyValueDictionary;
            }
        }
    
}
