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
        private DepthImagePixel[] depthImagePixels;
        private int recognizeCount = 0;
        private ColorImagePoint lastPoint;
        private Shape lastMarker;

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
                //sensor.DepthFrameReady += SensorOnDepthFrameReasy;

                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                sensor.SkeletonStream.EnableTrackingInNearRange = true;
                sensor.AllFramesReady += sensor_AllFramesReady;


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

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            depthImagePixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;
                frame.CopyDepthImagePixelDataTo(depthImagePixels);
            }

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;
                var bitmap = CreateBitmap(frame);
                ImageCanvas.Background = new ImageBrush(bitmap);
            }

            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                var skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                var skeleton = skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                if (skeleton == null)
                    return;

                var position = skeleton.Joints[JointType.HandRight].Position;
                var mapper = new CoordinateMapper(sensor);
                var colorPoint = mapper.MapSkeletonPointToColorPoint(position, ColorImageFormat.InfraredResolution640x480Fps30);
                var circle = CreateCircle(colorPoint);
                

                DetectChop(colorPoint, circle);
            }
        }

        private void DetectChop(ColorImagePoint colorPoint, Shape circle)
        {
            if(lastMarker != null)
            {
                ImageCanvas.Children.Remove(lastMarker);
            }

            if(recognizeCount == 0 
                && colorPoint.X > Canvas.GetLeft(currentShape) 
                && (colorPoint.X < Canvas.GetLeft(currentShape) + currentShape.Width)
                && colorPoint.Y > Canvas.GetTop(currentShape))
            {
                lastPoint = colorPoint;
                recognizeCount = 1;
                lastMarker = circle;
                ImageCanvas.Children.Add(circle);
                return;
            }

            if(recognizeCount > 0 && colorPoint.X > Canvas.GetLeft(currentShape)
                && (colorPoint.X < Canvas.GetLeft(currentShape) + currentShape.Width)
                && colorPoint.Y > lastPoint.Y)
            {
                recognizeCount++;
                lastMarker = circle;
                ImageCanvas.Children.Add(circle);
            }
            else
            {
                recognizeCount = 0;
            }

            if(recognizeCount > 6)
            {
                ImageCanvas.Children.Remove(currentShape);
                currentShape = MakeRectangle();
                ImageCanvas.Children.Add(currentShape);
                recognizeCount = 0;
            }
        }

        private Shape CreateCircle(ColorImagePoint colorPoint)
        {
            var circle = new Ellipse();
            circle.Fill = Brushes.Red;
            circle.Height = 20;
            circle.Width = 20;
            circle.Stroke = Brushes.Red;
            circle.StrokeThickness = 2;
            Canvas.SetLeft(circle, colorPoint.X);
            Canvas.SetTop(circle, colorPoint.Y);
            return circle;
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

        private BitmapSource CreateBitmap(ColorImageFrame frame)
        {
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);

            GrayscaleData(pixelData);

            var stride = frame.Width * frame.BytesPerPixel;
            var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            return bitmap;
        }

        private void GrayscaleData(byte[] pixelData)
        {
            var mapper = new CoordinateMapper(sensor);
            var depthPoints = new DepthImagePoint[640 * 480];
            mapper.MapColorFrameToDepthFrame(ColorImageFormat.InfraredResolution640x480Fps30, DepthImageFormat.Resolution640x480Fps30, depthImagePixels, depthPoints);

            for(int i = 0; i < depthPoints.Length; i++)
            {
                var point = depthPoints[i];
                if(point.Depth > 600 || !KinectSensor.IsKnownPoint(point))
                {
                    var pixelDataIndex = i * 4;
                    var max = Math.Max(pixelData[pixelDataIndex], Math.Max(pixelData[pixelDataIndex + 1], pixelData[pixelDataIndex + 2]));
                    pixelData[pixelDataIndex] = max;
                    pixelData[pixelDataIndex + 1] = max;
                    pixelData[pixelDataIndex + 2] = max;
                }
            }
        }
    }
}
