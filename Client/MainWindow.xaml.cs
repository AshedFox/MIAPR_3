using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int _offset = 100;
        private readonly int _pointsCount = 10000;
        private double _separationLine;
        private double _falseAlertError;
        private double _missingDetectionError;
        private double _totalError;
        private Point[] _points1 = Array.Empty<Point>();
        private Point[] _points2 = Array.Empty<Point>();

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            var probability1 = Convert.ToDouble(ProbabilityTextBox1.Text) / 100;
            var probability2 = Convert.ToDouble(ProbabilityTextBox2.Text) / 100;
            
            _points1 = new Point[_pointsCount];
            _points2 = new Point[_pointsCount];
            
            for (var i = 0; i < _pointsCount; i++)
            {
                _points1[i].X = Algorithm.GenerateRandomDouble(-_offset, 600 - _offset);
                _points2[i].X = Algorithm.GenerateRandomDouble(_offset, 600 + _offset);
            }
            
            SortPointsArray(ref _points1);
            SortPointsArray(ref _points2);

            var mathExpectation1 = Algorithm.CountMathExpectation(_points1.Select(v => v.X).ToList());
            var mathExpectation2 = Algorithm.CountMathExpectation(_points2.Select(v => v.X).ToList());

            var standardDeviation1 = Algorithm.CountStandardDeviation(mathExpectation1, _points1.Select(v => v.X).ToList());
            var standardDeviation2 = Algorithm.CountStandardDeviation(mathExpectation2, _points2.Select(v => v.X).ToList());

            (_falseAlertError, _separationLine) = Algorithm.CountFalseAlertError(-_offset, mathExpectation1, mathExpectation2,
                standardDeviation1, standardDeviation2, probability1, probability2);

            _missingDetectionError = Algorithm.CountMissingDetectionError(_separationLine, 750 + _offset, _falseAlertError,
                mathExpectation1, standardDeviation1, probability1);
            
            _totalError = _falseAlertError + _missingDetectionError;

            FalseAlarmErrorLabel.Content = $"Ложная тревога: {_falseAlertError}";
            MissingDetectionErrorLabel.Content = $"Пропуск обнаружения: {_missingDetectionError}";
            TotalErrorLabel.Content = $"Суммарная: {_totalError}";
            
            for (var i = 0; i < _pointsCount; i++)
            {
                _points1[i].Y = Algorithm.CountProbabilityDensity(mathExpectation1, standardDeviation1, _points1[i].X) *
                                probability1;
                _points2[i].Y = Algorithm.CountProbabilityDensity(mathExpectation2, standardDeviation2, _points2[i].X) *
                                probability2;
            }

            Draw();
        }

        private void SortPointsArray(ref Point[] points)
        {
            Array.Sort(points, (p1, p2) =>
            {
                if (Math.Abs(p1.X - p2.X) < double.Epsilon)
                {
                    return 0;
                } else if (p1.X > p2.X)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            });
        }

        private void DrawAxis()
        {
            var xAxisLine = new Line()
            {
                X1 = 0,
                Y1 = Chart.ActualHeight,
                X2 = Chart.ActualWidth,
                Y2 = Chart.ActualHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 5,
            };
            var yAxisLine = new Line()
            {
                X1 =  _offset,
                Y1 = 0,
                X2 =  _offset,
                Y2 = Chart.ActualHeight,
                Stroke = Brushes.Black,
                StrokeThickness = 5,
            };

            Chart.Children.Add(xAxisLine);
            Chart.Children.Add(yAxisLine);
        }

        private void DrawDivideLine()
        {
            var scaleX = new[]
            {
                Chart.ActualWidth / _points1.Max(p => p.X),
                Chart.ActualWidth / _points2.Max(p => p.X),
            }.Min() - 0.05;
            
            var divideLine = new Line()
            {
                X1 = _separationLine * scaleX,
                Y1 = 0,
                X2 = _separationLine * scaleX,
                Y2 = Chart.ActualHeight,
                Stroke = Brushes.Gray,
                StrokeDashArray = DoubleCollection.Parse("2 2"),
                StrokeThickness = 1,
            };

            Chart.Children.Add(divideLine);
        }

        private void DrawChart()
        {
            var scaleX = new[]
            {
                Chart.ActualWidth / _points1.Max(p => p.X),
                Chart.ActualWidth / _points2.Max(p => p.X),
            }.Min() - 0.05;

            var scaleY = new[]
            {
                Chart.ActualHeight / _points1.Max(p => p.Y),
                Chart.ActualHeight / _points2.Max(p => p.Y)
            }.Min() - 0.05;
            
            var textBlock1 = new TextBlock()
            {
                Text = "p(X/C1) * P(C1)",
                FontSize = 16
            };
            Canvas.SetLeft(textBlock1, _points1[_points1.Length / 2].X * scaleX);
            Canvas.SetTop(textBlock1, Chart.ActualHeight - _points1[_points1.Length / 2].Y * scaleY);
            Chart.Children.Add(textBlock1);
            
            for (var i = 0; i < _points1.Length - 1; i++)
            {
                var line = new Line()
                {
                    X1 = _points1[i].X * scaleX + _offset,
                    Y1 = Chart.ActualHeight - _points1[i].Y * scaleY,
                    X2 = _points1[i + 1].X * scaleX + _offset, 
                    Y2 = Chart.ActualHeight - _points1[i + 1].Y * scaleY,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1,
                };
                Chart.Children.Add(line);
            }

            var textBlock2 = new TextBlock()
            {
                Text = "p(X/C2) * P(C2)",
                FontSize = 16
            };
            Canvas.SetLeft(textBlock2, _points2[_points2.Length / 2].X * scaleX);
            Canvas.SetTop(textBlock2, Chart.ActualHeight - _points2[_points2.Length / 2].Y * scaleY);
            Chart.Children.Add(textBlock2);
            
            for (var i = 0; i < _points2.Length - 1; i++)
            {
                var line = new Line()
                {
                    X1 = _points2[i].X * scaleX + _offset,
                    Y1 = Chart.ActualHeight - _points2[i].Y * scaleY,
                    X2 = _points2[i + 1].X * scaleX + _offset,
                    Y2 = Chart.ActualHeight - _points2[i + 1].Y * scaleY,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1,
                };
                Chart.Children.Add(line);
            }
        }
        
        private void Draw()
        {
            Chart.Children.Clear();
            
            DrawAxis();

            if (_falseAlertError > 0 && _missingDetectionError > 0)
            {
                DrawDivideLine();
            }

            if (_points1.Length > 0 && _points2.Length > 0)
            {
                DrawChart();
            }
        }

        private void ProbabilityTextBox2_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ProbabilityTextBox1 is not null && ProbabilityTextBox2 is not null)
            {
                ProbabilityTextBox1.Text = ProbabilityTextBox2.Text.Length > 0
                    ? (100 - Convert.ToInt32(ProbabilityTextBox2.Text)).ToString(CultureInfo.CurrentCulture)
                    : "";
            }
        }

        private void ProbabilityTextBox1_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ProbabilityTextBox1 is not null && ProbabilityTextBox2 is not null)
            {
                ProbabilityTextBox2.Text = ProbabilityTextBox1.Text.Length > 0
                    ? (100 - Convert.ToInt32(ProbabilityTextBox1.Text)).ToString(CultureInfo.CurrentCulture)
                    : "";
            }
        }

        private void ProbabilityTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Chart_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Chart is not null)
            {
                Draw();
            }
        }
    }
}