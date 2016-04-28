using Microsoft.Kinect;
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
using Coding4Fun.Kinect.Wpf;

namespace ShapeNinjaKinectWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private Shape currentShape;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if(StartStopButton.Content.ToString() == "Start")
            {
                if(KinectSensor.KinectSensors.Count > 0)
                {
                    KinectSensor.KinectSensors.StatusChanged += (o, args) =>
                    {
                        Status.Content = args.Status.ToString();
                    };
                    sensor = KinectSensor.KinectSensors[0];
                }
                sensor.Start();
                ConnectionId.Content = sensor.DeviceConnectionId;
                
                sensor.ColorStream.Enable();

                currentShape = MakeRectangle();
                ImageCanvas.Children.Add(currentShape);

                //sensor.ColorFrameReady += SensorOnColorFrameReady;

                sensor.DepthStream.Enable();
                sensor.DepthFrameReady += SensorOnDepthFrameReasy;

                sensor.SkeletonStream.Enable();


                StartStopButton.Content = "Stop";
            }
            else
            {
                if(sensor != null && sensor.IsRunning)
                {
                    sensor.Stop();
                    StartStopButton.Content = "Start";
                }

                ImageCanvas.Children.Remove(currentShape);
            }
        }

        private void SensorOnDepthFrameReasy(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (var frame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                //ImageCanvas.Background = new ImageBrush(frame.ToBitmapSource());
                var depthImagePixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                frame.CopyDepthImagePixelDataTo(depthImagePixels);

                var colorPixels = new byte[4 * sensor.DepthStream.FramePixelDataLength];
                for(int i = 0; i < colorPixels.Length; i += 4)
                {
                    if(depthImagePixels[i / 4].PlayerIndex != 0)
                    {
                        colorPixels[i + 1] = 255;
                    }
                }

                ImageCanvas.Background = new ImageBrush(colorPixels.ToBitmapSource(640, 480));
            }
        }

        private Rectangle MakeRectangle()
        {
            var rectangle = new Rectangle();
            rectangle.Stroke = Brushes.Blue;
            rectangle.Width = 100;
            rectangle.Height = 100;
            rectangle.StrokeThickness = 2;
            rectangle.Fill = Brushes.Blue;

            var random = new Random();
            Canvas.SetLeft(rectangle, random.Next((int) (ImageCanvas.Width - 100)));
            Canvas.SetTop(rectangle, random.Next((int) (ImageCanvas.Height - 100)));
            return rectangle;
        }

        private void SensorOnColorFrameReady(object sender, ColorImageFrameReadyEventArgs colorImageFrameReadyEventArgs)
        {
            using (var frame = colorImageFrameReadyEventArgs.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    BitmapSource bitmap = CreateBitmap(frame);
                    ImageCanvas.Background = new ImageBrush(bitmap);
                    //ImageCanvas.Background = new ImageBrush(frame.ToBitmapSource());
                }
            }
        }

        private void TakePictureButton_Click(object sender, RoutedEventArgs e)
        {
            using (var frame = sensor.ColorStream.OpenNextFrame(0))
            {
                BitmapSource bitmap = CreateBitmap(frame);
                ImageCanvas.Background = new ImageBrush(bitmap);
            }
        }

        private static BitmapSource CreateBitmap(ColorImageFrame frame)
        {
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);

            GrayscaleData(pixelData);

            var stride = frame.Width * frame.BytesPerPixel;
            var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            return bitmap;
        }

        private static void GrayscaleData(byte[] pixelData)
        {
            for(int i = 0; i < pixelData.Length; i += 4)
            {
                var max = Math.Max(pixelData[i], Math.Max(pixelData[i + 1], pixelData[i + 2]));
                pixelData[i] = max;
                pixelData[i + 1] = max;
                pixelData[i + 2] = max;
            }
        }
    }
}
