using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace convex_hull_visualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public Point[] points = new Point[0];
    public Stack<Point> hullPoints = new Stack<Point>();

    public MainWindow()
    {
        InitializeComponent();
    }

    public struct Point(double x, double y)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Angle { get; set; }
    }

    public void DrawPoints()
    {
        Color[] colors = [Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Orange];
        foreach (var point in points)
        {
            int index = (int)point.Y % colors.Length;

            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(colors[index])
            };

            Canvas.SetLeft(ellipse, point.X);
            Canvas.SetTop(ellipse, point.Y);

            MyCanvas.Children.Add(ellipse);
        }
    }

    private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        double x = e.GetPosition(MyCanvas).X;
        double y = e.GetPosition(MyCanvas).Y;

        Point clickPosition = new Point(x, y);

        Point[] newPoints = new Point[points.Length + 1];

        for (int i = 0; i < points.Length; i++)
        {
            newPoints[i] = points[i];
        }

        newPoints[points.Length] = clickPosition;

        points = newPoints;

        DrawPoints();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void GeneratePoints(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Generating points";
    }

    private void ImportCSV(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Importing points";
    }

    private void CalculateConvexHull(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Calculating";
    }
}
