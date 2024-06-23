using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;

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

        ExportButton.IsEnabled = false;
        GenerateButton.IsEnabled = false;
        RunButton.IsEnabled = false;
    }

    ///<summary>
    /// Structure representing point on the plane.
    ///</summary>
    public struct Point
    {
        public Point(double x, double y)
        {
            X = x;
            Y = y;

            Random random = new Random();

            Color = Color.FromRgb(
                (byte)random.Next(0, 200),
                (byte)random.Next(0, 200),
                (byte)random.Next(0, 200)
            );
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }

        public string Print()
        {
            return "(" + (int)X + " , " + (int)Y + ") " + (int)Angle + " \n";
        }

        public Color Color { get; set; }
    }

    ///<summary>
    /// Structure representing a point user for csv serialization.
    ///</summary>
    public struct CSVPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    ///<summary>
    /// Draws already added points on the canvas.
    ///</summary>
    public void DrawPoints()
    {
        MyCanvas.Children.Clear();

        DrawScale();

        foreach (var point in points)
        {
            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(point.Color),
            };

            Canvas.SetLeft(ellipse, point.X - 5);
            Canvas.SetTop(ellipse, point.Y - 5);
            MyCanvas.Children.Add(ellipse);
        }
    }

    ///<summary>
    /// Draws the X and Y axis on the canvas.
    ///</summary>
    private void DrawScale()
    {
        //The width and heieght of the canvas
        int scaleWidth = (int)Border.ActualWidth;
        int scaleHeight = (int)Border.ActualHeight;

        //The distances between ticks
        int majorTickInterval = 100;
        int minorTickInterval = 20;

        //The Y offset of the X axis
        int Y = 2;
        //The X offset of the Y axis
        int X = 2;

        //X axis

        Line xAxis = new Line
        {
            X1 = 0,
            Y1 = Y,
            X2 = scaleWidth,
            Y2 = Y,
            Stroke = Brushes.Gray,
            StrokeThickness = 2
        };

        MyCanvas.Children.Add(xAxis);

        //Draw major ticks
        for (int i = 0; i <= scaleWidth; i += majorTickInterval)
        {
            Line tick = new Line
            {
                X1 = i,
                Y1 = Y - 8,
                X2 = i,
                Y2 = Y + 8,
                Stroke = Brushes.Gray,
                StrokeThickness = 2
            };
            MyCanvas.Children.Add(tick);

            //Darw labels over them
            TextBlock label = new TextBlock { Text = i == 0 ? "0" : i.ToString(), FontSize = 12 };
            label.RenderTransform = new ScaleTransform(1, -1);

            Canvas.SetLeft(label, i == 0 ? 16 : (i - 10));
            Canvas.SetTop(label, Y + 25);

            MyCanvas.Children.Add(label);
        }

        //Draw minor ticks
        for (int i = 0; i <= scaleWidth; i += minorTickInterval)
        {
            if (i % majorTickInterval != 0) //Don't draw on major ticks
            {
                Line tick = new Line
                {
                    X1 = i,
                    Y1 = Y - 5,
                    X2 = i,
                    Y2 = Y + 5,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                MyCanvas.Children.Add(tick);
            }
        }

        //Y Axis

        Line yAxis = new Line
        {
            X1 = X,
            Y1 = 0,
            X2 = X,
            Y2 = scaleHeight,
            Stroke = Brushes.Gray,
            StrokeThickness = 2
        };
        MyCanvas.Children.Add(yAxis);

        //Draw major ticks
        for (double i = 0; i <= scaleHeight; i += majorTickInterval)
        {
            Line tick = new Line
            {
                X1 = X - 8,
                Y1 = i,
                X2 = X + 8,
                Y2 = i,
                Stroke = Brushes.Gray,
                StrokeThickness = 2
            };
            MyCanvas.Children.Add(tick);

            //Draw label beside them
            TextBlock label = new TextBlock
            {
                Text = i == 0 ? "" : i.ToString(),
                FontSize = 12,
                RenderTransform = new ScaleTransform(1, -1)
            };

            Canvas.SetLeft(label, X + 15);
            Canvas.SetTop(label, i + 8);

            MyCanvas.Children.Add(label);
        }

        //Draw minor ticks
        for (double i = 0; i <= scaleHeight; i += minorTickInterval)
        {
            if (Math.Abs(i % majorTickInterval) > 0.001) // Don't draw over major ticks
            {
                Line tick = new Line
                {
                    X1 = X - 5,
                    Y1 = i,
                    X2 = X + 5,
                    Y2 = i,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };

                MyCanvas.Children.Add(tick);
            }
        }
    }

    ///<summary>
    /// Redraws points, scales and lines. Used after window resize event.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void Resized(object sender, SizeChangedEventArgs e)
    {
        DrawPoints();

        if (hullPoints.Count > 0)
        {
            DrawLines();
        }
    }

    ///<summary>
    /// Handles the left mouse button click. Adds a point to the list of points from the position on the canvas.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //Get position of the click
        double x = e.GetPosition(MyCanvas).X;
        double y = e.GetPosition(MyCanvas).Y;

        Point clickPosition = new Point(x, y);

        //Create a new array of points and copy the new ones
        Point[] newPoints = new Point[points.Length + 1];

        for (int i = 0; i < points.Length; i++)
        {
            newPoints[i] = points[i];
        }

        //Add the new points at the end
        newPoints[points.Length] = clickPosition;

        //Switch the two arrays
        points = newPoints;

        RunButton.IsEnabled = true;

        DrawPoints();
    }

    ///<summary>
    /// Draws the lines of the hull on the canvas.
    ///</summary>
    public void DrawLines()
    {
        MyCanvas.Children.Clear();
        DrawPoints();

        Point[] hullArray = hullPoints.ToArray();

        //Iterates over all hull points and draws lines between the current and the previous point
        for (int i = 1; i < hullArray.Length; i++)
        {
            Line line = new Line
            {
                X1 = hullArray[i - 1].X,
                Y1 = hullArray[i - 1].Y,
                X2 = hullArray[i].X,
                Y2 = hullArray[i].Y,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            MyCanvas.Children.Add(line);
        }
    }

    ///<summary>
    /// Clears added points, hull points, the canvas, the output and buttons behavior.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void Clear(object sender, RoutedEventArgs e)
    {
        points = new Point[0];
        hullPoints = new List<Point>();
        MyCanvas.Children.Clear();
        ExportButton.IsEnabled = false;
        NumberTextBox.Text = "";
        GenerateButton.IsEnabled = false;
        RunButton.IsEnabled = false;

        Resized(null, null);

        OutputText.Text = "Cleared";
    }

    ///<summary>
    /// Validates if the input character is a number.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        //Check with Regex if it's a number
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);

        if (!e.Handled)
        {
            GenerateButton.IsEnabled = true;
        }
    }

    ///<summary>
    /// Checks if string in the NumberTextBox is empty and enables GenerateButton accordingly.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void CheckIfEmpty(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Back && NumberTextBox.Text.Length == 0)
        {
            GenerateButton.IsEnabled = false;
        }
    }

    ///<summary>
    /// Generates a number specified in the NumberTextBox of random points and adds them to the existing points.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void GeneratePoints(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Generating points";

        //Specifies the max value for X and Y
        int maxHight = (int)Border.ActualHeight - 10;
        int maxWidth = (int)Border.ActualWidth - 10;

        Random random = new Random();

        for (int i = 0; i < int.Parse(NumberTextBox.Text); i++)
        {
            //Create a new random point
            Point newPoint = new Point(random.Next(maxWidth), random.Next(maxHight));

            Point[] newPoints = new Point[points.Length + 1];

            //Copy the old points into a new array
            for (int j = 0; j < points.Length; j++)
            {
                newPoints[j] = points[j];
            }

            //Add the new point
            newPoints[points.Length] = newPoint;

            //Switch the arrays
            points = newPoints;
        }

        DrawPoints();
        RunButton.IsEnabled = true;
    }

    ///<summary>
    /// Imports a specified by the user CSV file and parses the content to add it to the existing points.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void ImportCSV(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Importing points \n";

        //Dialog for choosing a file
        OpenFileDialog openFileDialog = new OpenFileDialog();

        openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        openFileDialog.FilterIndex = 1;
        openFileDialog.Multiselect = false;

        bool? userClickedOK = openFileDialog.ShowDialog();

        //If dialog open
        if (userClickedOK == true)
        {
            //CSV parser config
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                NewLine = ";",
                HasHeaderRecord = false
            };

            string filePath = openFileDialog.FileName;

            //Read The CSV
            var reader = new StreamReader(filePath);
            var csv = new CsvReader(reader, config);

            //Try to parse the read file
            try
            {
                Clear(null, null);
                while (csv.Read())
                {
                    CSVPoint newPoint = csv.GetRecord<CSVPoint>();

                    Point[] newPoints = new Point[points.Length + 1];

                    for (int i = 0; i < points.Length; i++)
                    {
                        newPoints[i] = points[i];
                    }

                    newPoints[points.Length] = new Point(newPoint.X, newPoint.Y);

                    points = newPoints;
                }

                if (points.Length > 0)
                {
                    RunButton.IsEnabled = true;
                }

                DrawPoints();
                OutputText.Text = "Imported! \n";
            }
            catch (Exception Error) //Catch exceptions if they occured
            {
                OutputText.Text = "There is something wrong with the CSV file! \n";
                return;
            }
        }
    }

    ///<summary>
    /// Exports the result (hull points) to a specified by user CSV files.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>

    private void ExportCSV(object sender, RoutedEventArgs e)
    {
        OutputText.Text = "Exporting points";

        //Config for the parser
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            NewLine = ";\n",
            HasHeaderRecord = false
        };

        var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        saveFileDialog.FilterIndex = 1;
        saveFileDialog.RestoreDirectory = true;

        if (saveFileDialog.ShowDialog() == true)
        {
            using var writer = new StreamWriter(saveFileDialog.FileName);
            using var csv = new CsvWriter(writer, config);

            CSVPoint[] exportData = new CSVPoint[hullPoints.Count];
            int i = 0;

            foreach (var point in hullPoints)
            {
                exportData[i] = new CSVPoint { X = point.X, Y = point.Y };
                i++;
            }

            //Write to file
            csv.WriteRecords(exportData);

            MessageBox.Show(
                "Data exported successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        OutputText.Text = "Points exported! \n";
    }

    ///<summary>
    /// Calculates the convex hull based on the added points. Uses the Graham algorithm.
    /// <param name="sender">Object representing the sender of the event.</param>
    /// <param name="e"> Arguments passed from that event.</param>
    ///</summary>
    private void CalculateConvexHull(object sender, RoutedEventArgs e)
    {
        //Check if the algorithm has enough points
        if (AreTwoPointsDiffrent())
        {
            OutputText.Text = "Calculated a point \n";
            return;
        }

        OutputText.Text = "Calculating \n";
        hullPoints = new List<Point>();

        //Sort points along the Y axis and take the first one (lowest point)
        Point lowestPoint = points.OrderBy(p => p.Y).ToArray().First();

        //Calculate the angle between the lowest point and the others
        for (int i = 0; i < points.Length; i++)
        {
            points[i].Angle = CalcAngleBetweensPointsAndXAxis(lowestPoint, points[i]);
        }

        //Exclude the lowest point
        Point[] filteredPoints = points
            .Where(p => !(p.X == lowestPoint.X && p.Y == lowestPoint.Y))
            .ToArray();

        //Sort them by the calcualted angle
        Point[] angleSortedPoints = filteredPoints.OrderBy(p => p.Angle).ToArray();

        OutputText.Text = "";

        //Add the lowest point (is guaranteed to be in the hull) and the next point
        hullPoints.Add(lowestPoint);
        hullPoints.Add(angleSortedPoints[0]);

        for (int i = 1; i < angleSortedPoints.Length; i++)
        {
            if (hullPoints.Count >= 2) //If there is enough points for the algorithm to continue
            {
                //check if the angle between the penultimate, previous and the current point is counterclockwise
                int isCCW = IsCounterClockWise(
                    hullPoints[hullPoints.Count - 2],
                    hullPoints[hullPoints.Count - 1],
                    angleSortedPoints[i]
                );

                if (isCCW == 1) //Is counterclockwise
                {
                    //Add current point to hull points
                    hullPoints.Add(angleSortedPoints[i]);
                }
                else if (isCCW == -1) // Is clockwise
                {
                    //Remove the previous point from the hull points and decrement the counter
                    //tldr; "move one back"
                    hullPoints.RemoveAt(hullPoints.Count - 1);
                    i--;
                }
                else
                { //The previous point is on the line -> ignore
                }
            }
        }

        //Add the first point once more for simplicity
        hullPoints.Add(lowestPoint);

        //Informs the user what shape has been created
        OutputText.Text += "Calculated ";

        if (hullPoints.Count == 3)
            OutputText.Text += "a line!";
        else if (hullPoints.Count == 4)
            OutputText.Text += "a triangle!";
        else if (hullPoints.Count == 5)
            OutputText.Text += "a quadrilateral!";
        else
            OutputText.Text += "complex convex figure!";

        //If there are 5 or less points print them in order
        if (hullPoints.Count <= 5)
        {
            OutputText.Text += "\n\nIn order: \n";
            for (int i = 0; i < hullPoints.Count - 1; i++)
            {
                OutputText.Text +=
                    "("
                    + hullPoints[i].X.ToString("0.00")
                    + ", "
                    + hullPoints[i].Y.ToString("0.00")
                    + ") \n";
            }
        }

        DrawLines();

        ExportButton.IsEnabled = true;
    }

    ///<summary>
    /// Calcualtes the angle between a point and the X axis.
    /// <param name="a">First point (should be lowest).</param>
    /// <param name="b">Second point.</param>
    /// <returns>The angle between a and b</returns>
    ///</summary>
    double CalcAngleBetweensPointsAndXAxis(Point a, Point b)
    {
        //If both are the same point the angle is 0
        if (a.X == b.X && a.Y == b.Y)
        {
            return 0;
        }

        //Distance between the points
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;

        // Arcus tangens of the distances in degrees
        return Math.Atan2(dy, dx) * (180 / Math.PI);
    }

    ///<summary>
    /// Checks if the angle between 3 points is counterclockwise or not.
    /// <param name="a">First point (penultimate point)</param>
    /// <param name="b">Second point (previous point).</param>
    /// <param name="c">Third point (current point).</param>
    /// <returns>1 if is counterclockwise, -1 if is clockwise and 0 if is a straight line</returns>
    ///</summary>
    public int IsCounterClockWise(Point a, Point b, Point c)
    {
        //Signed area of a parrallelogram spaning on the points a,b,c
        double result = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        if (result > 0) //result is positive -> is counterclockwise
        {
            return 1;
        }
        else if (result < 0) // result is negatieve -> clockwise
        {
            return -1;
        }

        return 0; // area is 0 -> points are on a line
    }

    ///<summary>
    /// Checks if there are enough original points for the algorithm to run.
    /// <returns>True if there are at least 2 original points, False otherwise</returns>
    ///</summary>
    public bool AreTwoPointsDiffrent()
    {
        //There are 0 or 1 points
        if (points.Length < 2)
            return true;

        //Compare all points against each other to find original points
        for (int i = 0; i < points.Length; i++)
        {
            for (int j = i + 1; j < points.Length; j++)
            {
                if (points[i].X != points[j].X || points[i].Y != points[j].Y)
                {
                    //There are original points -> exit prematurely
                    return false;
                }
            }
        }

        //Return true otherwise
        return true;
    }
}
