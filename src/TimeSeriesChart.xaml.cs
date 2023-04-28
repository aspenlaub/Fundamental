using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental;

/// <summary>
/// Interaction logic for TimeSeriesChart.xaml
/// </summary>
// ReSharper disable once UnusedMember.Global
public partial class TimeSeriesChart {
    private const double ChartMargin = 20, BlockWidth = 150, BlockHeight = 75;

    public ITimeSeriesChartApplication Application { get; set; }
    public uint UniqueChartNumber { get; set; }
    // ReSharper disable once UnusedMember.Global
    public string Title { set => TitleBlock.Text = value; }

    public TimeSeriesChart() {
        InitializeComponent();
        TitleBlock.Margin = new Thickness(ChartMargin, 2, 0, 0);
    }

    private async void OnZoomOutClickAsync(object sender, RoutedEventArgs e) {
        await Application.ZoomOutAsync(UniqueChartNumber);
    }

    private async void OnZoomInClickAsync(object sender, RoutedEventArgs e) {
        await Application.ZoomInAsync(UniqueChartNumber);
    }

    private async void OnScrollLeftClickAsync(object sender, RoutedEventArgs e) {
        await Application.ScrollLeftAsync(UniqueChartNumber);
    }

    private async void OnScrollRightClickAsync(object sender, RoutedEventArgs e) {
        await Application.ScrollRightAsync(UniqueChartNumber);
    }

    // ReSharper disable once UnusedMember.Global
    public void Draw() {
        if (Canvas.ActualWidth <= 0 || Canvas.ActualHeight <= 0) { return; }

        var visiblePoints = Application.VisibleChartPoints(UniqueChartNumber);
        if (!visiblePoints.Any()) { return; }

        Canvas.Children.Clear();
        var minDate = visiblePoints.Keys.First();
        var maxDate = visiblePoints.Keys.Last();
        var visibleNumberOfDays = (maxDate - minDate).Days;
        var maxValue = visiblePoints.Values.Max();
        maxValue = RoundValue(maxValue, maxValue);
        var minValue = RoundValue(visiblePoints.Values.Min(), maxValue);
        var valueSpan = maxValue - minValue;
        double x = 0, y = 0;
        var first = true;
        var width = Canvas.ActualWidth - ChartMargin;
        var height = Canvas.ActualHeight - 2 * ChartMargin;
        var horizontalBlocks = Math.Floor(width / BlockWidth);
        var blockWidth = Math.Round(width / horizontalBlocks, 0);
        var verticalBlocks = Math.Floor(height / BlockHeight);
        var blockHeight = Math.Round(height / verticalBlocks, 0);
        var label = new TextBlock() { Text = minValue.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
        Canvas.SetBottom(label, ChartMargin);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = maxValue.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
        Canvas.SetTop(label, ChartMargin);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = minDate.ToString("dd.MM.yyyy") };
        Canvas.SetLeft(label, ChartMargin);
        Canvas.SetBottom(label, 0);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = maxDate.ToString("dd.MM.yyyy") };
        Canvas.SetRight(label, 0);
        Canvas.SetBottom(label, 0);
        Canvas.Children.Add(label);
        for (var i = 0; i < horizontalBlocks; i++) {
            var lighter = i % 2 == 0;
            for (var j = 0; j < verticalBlocks; j++) {
                double x1 = ChartMargin + i * blockWidth, x2 = i + 1 < horizontalBlocks ? ChartMargin + (i + 1) * blockWidth : width + ChartMargin;
                double y1 = ChartMargin + j * blockHeight, y2 = j + 1 < verticalBlocks ? ChartMargin + (j + 1) * blockHeight : height + ChartMargin;
                var rectangle = new Rectangle() { Width = x2 - x1, Height = y2 - y1, Fill = new SolidColorBrush(Colors.LightGray), Opacity = lighter ? 0.1 : 0.2 };
                Canvas.SetLeft(rectangle, x1);
                Canvas.SetBottom(rectangle, y1);
                Canvas.Children.Add(rectangle);
                lighter = !lighter;
            }
        }
        for (var i = 1; i < horizontalBlocks; i++) {
            var date = minDate.AddDays((maxDate - minDate).Days * i / (horizontalBlocks + 1));
            label = new TextBlock() { Text = date.ToString("dd.MM.yyyy") };
            Canvas.SetLeft(label, ChartMargin + i * blockWidth);
            Canvas.SetBottom(label, 0);
            Canvas.Children.Add(label);
        }
        for (var i = 1; i < verticalBlocks; i++) {
            var value = RoundValue(minValue + (maxValue - minValue) * i / (verticalBlocks + 1), maxValue);
            label = new TextBlock() { Text = value.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
            Canvas.SetBottom(label, ChartMargin + i * blockHeight);
            Canvas.Children.Add(label);
        }
        foreach (var visiblePoint in visiblePoints) {
            var oldX = x;
            var oldY = y;
            x = ChartMargin + (visiblePoint.Key - minDate).Days * width / visibleNumberOfDays;
            y = height + ChartMargin - (visiblePoint.Value - minValue) * height / valueSpan;
            if (first) {
                first = false;
                continue;
            }

            var line = new Line() { Stroke = TitleBlock.Foreground, X1 = oldX, Y1 = oldY, X2 = x, Y2 = y, StrokeThickness = 2 };
            Canvas.Children.Add(line);
        }
    }

    private static double RoundValue(double value, double maxValue) {
        return Math.Round(value, maxValue > 1000 ? 0 : 2);
    }
}