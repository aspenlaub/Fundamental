using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

// ReSharper disable AsyncVoidEventHandlerMethod

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental;

/// <summary>
/// Interaction logic for TimeSeriesChart.xaml
/// </summary>
// ReSharper disable once UnusedMember.Global
public partial class TimeSeriesChart {
    private const double _chartMargin = 20, _blockWidth = 150, _blockHeight = 75;

    public ITimeSeriesChartApplication Application { get; set; }
    public uint UniqueChartNumber { get; set; }
    // ReSharper disable once UnusedMember.Global
    public string Title {
        set { TitleBlock.Text = value; }
    }

    public TimeSeriesChart() {
        InitializeComponent();
        TitleBlock.Margin = new Thickness(_chartMargin, 2, 0, 0);
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

        IDictionary<DateTime, double> visiblePoints = Application.VisibleChartPoints(UniqueChartNumber);
        if (!visiblePoints.Any()) { return; }

        Canvas.Children.Clear();
        DateTime minDate = visiblePoints.Keys.First();
        DateTime maxDate = visiblePoints.Keys.Last();
        int visibleNumberOfDays = (maxDate - minDate).Days;
        double maxValue = visiblePoints.Values.Max();
        maxValue = RoundValue(maxValue, maxValue);
        double minValue = RoundValue(visiblePoints.Values.Min(), maxValue);
        double valueSpan = maxValue - minValue;
        double x = 0, y = 0;
        bool first = true;
        double width = Canvas.ActualWidth - _chartMargin;
        double height = Canvas.ActualHeight - 2 * _chartMargin;
        double horizontalBlocks = Math.Floor(width / _blockWidth);
        double blockWidth = Math.Round(width / horizontalBlocks, 0);
        double verticalBlocks = Math.Floor(height / _blockHeight);
        double blockHeight = Math.Round(height / verticalBlocks, 0);
        var label = new TextBlock() { Text = minValue.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
        Canvas.SetBottom(label, _chartMargin);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = maxValue.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
        Canvas.SetTop(label, _chartMargin);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = minDate.ToString("dd.MM.yyyy") };
        Canvas.SetLeft(label, _chartMargin);
        Canvas.SetBottom(label, 0);
        Canvas.Children.Add(label);
        label = new TextBlock() { Text = maxDate.ToString("dd.MM.yyyy") };
        Canvas.SetRight(label, 0);
        Canvas.SetBottom(label, 0);
        Canvas.Children.Add(label);
        for (int i = 0; i < horizontalBlocks; i++) {
            bool lighter = i % 2 == 0;
            for (int j = 0; j < verticalBlocks; j++) {
                double x1 = _chartMargin + i * blockWidth, x2 = i + 1 < horizontalBlocks ? _chartMargin + (i + 1) * blockWidth : width + _chartMargin;
                double y1 = _chartMargin + j * blockHeight, y2 = j + 1 < verticalBlocks ? _chartMargin + (j + 1) * blockHeight : height + _chartMargin;
                var rectangle = new Rectangle() { Width = x2 - x1, Height = y2 - y1, Fill = new SolidColorBrush(Colors.LightGray), Opacity = lighter ? 0.1 : 0.2 };
                Canvas.SetLeft(rectangle, x1);
                Canvas.SetBottom(rectangle, y1);
                Canvas.Children.Add(rectangle);
                lighter = !lighter;
            }
        }
        for (int i = 1; i < horizontalBlocks; i++) {
            DateTime date = minDate.AddDays((maxDate - minDate).Days * i / (horizontalBlocks + 1));
            label = new TextBlock() { Text = date.ToString("dd.MM.yyyy") };
            Canvas.SetLeft(label, _chartMargin + i * blockWidth);
            Canvas.SetBottom(label, 0);
            Canvas.Children.Add(label);
        }
        for (int i = 1; i < verticalBlocks; i++) {
            double value = RoundValue(minValue + (maxValue - minValue) * i / (verticalBlocks + 1), maxValue);
            label = new TextBlock() { Text = value.ToString(CultureInfo.CurrentCulture), LayoutTransform = new RotateTransform(-90) };
            Canvas.SetBottom(label, _chartMargin + i * blockHeight);
            Canvas.Children.Add(label);
        }
        foreach (KeyValuePair<DateTime, double> visiblePoint in visiblePoints) {
            double oldX = x;
            double oldY = y;
            x = _chartMargin + (visiblePoint.Key - minDate).Days * width / visibleNumberOfDays;
            y = height + _chartMargin - (visiblePoint.Value - minValue) * height / valueSpan;
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