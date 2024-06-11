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
    public List<Point> hullPoints = new List<Point>();

    public MainWindow()
    {
        InitializeComponent();
    }

    public struct Point(double x, double y)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Angle { get; set; }

        public string Print()
        {
            return "(" + (int)X + " , " + (int)Y + ") " + (int)Angle + " \n";
        }
    }

    public void DrawPoints()
    {
        MyCanvas.Children.Clear();

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

            Canvas.SetLeft(ellipse, point.X - 5);
            Canvas.SetTop(ellipse, point.Y - 5);

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

    public void DrawLines()
    {
        MyCanvas.Children.Clear();
        DrawPoints();

        Point[] hullArray = hullPoints.ToArray();

        for (int i = 1; i < hullArray.Length; i++)
        {
            Line line = new Line
            {
                X1 = hullArray[i - 1].X,
                Y1 = hullArray[i - 1].Y,
                X2 = hullArray[i].X,
                Y2 = hullArray[i].Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            MyCanvas.Children.Add(line);
        }
    }

    private void ClearPoints(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Clearing";
        points = new Point[0];
        hullPoints = new List<Point>();
        MyCanvas.Children.Clear();
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

    private void ExportCSV(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Exporting points";
    }

    public void PrintOutput() { }

    private void CalculateConvexHull(object sender, RoutedEventArgs e)
    {
        if (AreTwoPointsDiffrent())
        {
            OutputText.Text = "Not enough original points! \n";
            return;
        }

        OutputText.Text = "Calculating \n";
        hullPoints = new List<Point>();

        Point lowestPoint = points.OrderBy(p => p.Y).ToArray().First();

        for (int i = 0; i < points.Length; i++)
        {
            points[i].Angle = CalcAngleBetweensPointsAndXAxis(lowestPoint, points[i]);

            OutputText.Text += points[i].Print();
        }

        Point[] filteredPoints = points
            .Where(p => !(p.X == lowestPoint.X && p.Y == lowestPoint.Y))
            .ToArray();

        Point[] angleSortedPoints = filteredPoints.OrderBy(p => p.Angle).ToArray();

        OutputText.Text = "";

        hullPoints.Add(lowestPoint);
        hullPoints.Add(angleSortedPoints[0]);

        for (int i = 1; i < angleSortedPoints.Length; i++)
        {
            if (hullPoints.Count >= 2)
            {
                int isCCW = IsCounterClockWise(
                    hullPoints[hullPoints.Count - 2],
                    hullPoints[hullPoints.Count - 1],
                    angleSortedPoints[i]
                );

                OutputText.Text = isCCW + "  \n";

                if (isCCW == 1)
                {
                    hullPoints.Add(angleSortedPoints[i]);
                }
                else if (isCCW == -1)
                {
                    hullPoints.RemoveAt(hullPoints.Count - 1);
                    i--;
                }
                else
                { //on the same line
                }
            }
        }
        hullPoints.Add(lowestPoint);

        OutputText.Text += "Calculated ";
        if (hullPoints.Count == 3)
            OutputText.Text += "a line!";
        else if (hullPoints.Count == 4)
            OutputText.Text += "a triangle!";
        else if (hullPoints.Count == 5)
            OutputText.Text += "a square!";
        else
            OutputText.Text += "something!";

        DrawLines();
    }

    double CalcAngleBetweensPointsAndXAxis(Point a, Point b)
    {
        if (a.X == b.X && a.Y == b.Y)
        {
            return 0;
        }

        double dx = b.X - a.X;
        double dy = b.Y - a.Y;

        return Math.Atan2(dy, dx) * (180 / Math.PI);
    }

    public int IsCounterClockWise(Point a, Point b, Point c)
    {
        double result = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        if (result > 0)
        {
            return 1;
        }
        else if (result < 0)
        {
            return -1;
        }

        return 0;
    }

    public bool AreTwoPointsDiffrent()
    {
        OutputText.Text = points.Length + "\n";
        foreach (Point p in points)
        {
            OutputText.Text += p.Print() + "\n";
        }

        if (points.Length < 2)
            return true;

        for (int i = 0; i < points.Length; i++)
        {
            for (int j = i + 1; j < points.Length; j++)
            {
                if (points[i].X != points[j].X || points[i].Y != points[j].Y)
                {
                    OutputText.Text += "found one \n";
                    return false;
                }
            }
        }

        return true;
    }
}
