using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Calculation;

public class TimeSeriesChartSource {
    protected SortedDictionary<DateTime, double> ChartPoints;
    protected uint ZoomFactor;
    private DateTime? StartDate { get; set; }
    private TimeSpan? VisibleTimeSpan { get; set; }

    public TimeSeriesChartSource() {
        ChartPoints = new SortedDictionary<DateTime, double>();
        ZoomFactor = 1;
        Clear();
    }

    public void Clear() {
        lock (ChartPoints) {
            ChartPoints.Clear();
        }
    }

    public void AddPoint(DateTime date, double value) {
        lock (ChartPoints) {
            ChartPoints[date] = value;
        }
    }

    public IDictionary<DateTime, double> VisiblePoints() {
        IDictionary<DateTime, double> visiblePoints;
        lock (ChartPoints) {
            UpdateTimeSpan();
            UpdateStartDate();
            visiblePoints = ChartPoints.Where(x => x.Key >= StartDate && x.Key <= StartDate + VisibleTimeSpan).ToDictionary(x => x.Key, x => x.Value);
        }
        return visiblePoints;
    }

    protected void UpdateTimeSpan() {
        TimeSpan? visibleTimeSpan = null;
        if (ChartPoints.Any()) {
            visibleTimeSpan = TimeSpan.FromDays(Math.Floor((double)(ChartPoints.Keys.Last() - ChartPoints.Keys.First()).Days / ZoomFactor));
        }
        if (visibleTimeSpan == VisibleTimeSpan) { return; }

        VisibleTimeSpan = visibleTimeSpan;
        UpdateStartDate();
    }

    protected void UpdateStartDate() {
        DateTime? startDate = null;
        if (ChartPoints.Any()) {
            startDate = StartDate;
            if (startDate == null || startDate < ChartPoints.Keys.First()) {
                startDate = ChartPoints.Keys.First();
            } else {
                var minStartDate = ChartPoints.Keys.Last() - VisibleTimeSpan;
                if (startDate > minStartDate) {
                    startDate = minStartDate;
                }
            }
        }
        StartDate = startDate;
    }

    public void ZoomIn() {
        if (!ChartPoints.Any()) { return; }

        ZoomFactor++;
        UpdateTimeSpan();
    }

    public void ZoomOut() {
        if (!ChartPoints.Any()) { return; }
        if (ZoomFactor == 1) { return; }

        ZoomFactor--;
        UpdateTimeSpan();
    }

    public void ScrollRight() {
        if (!ChartPoints.Any()) { return; }
        if (VisibleTimeSpan == null || StartDate == null) { return; }

        var timeSpan = (TimeSpan)VisibleTimeSpan;
        var date = ((DateTime)StartDate).AddDays(Math.Floor((double)timeSpan.Days / 2));
        StartDate = date;
        UpdateStartDate();
    }

    public void ScrollLeft() {
        if (!ChartPoints.Any()) { return; }
        if (VisibleTimeSpan == null || StartDate == null) { return; }

        var timeSpan = (TimeSpan)VisibleTimeSpan;
        var date = ((DateTime)StartDate).AddDays(- Math.Floor((double)timeSpan.Days / 2));
        StartDate = date;
        UpdateStartDate();
    }
}