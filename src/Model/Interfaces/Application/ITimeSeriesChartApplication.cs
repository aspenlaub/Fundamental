using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces.Application;

public interface ITimeSeriesChartApplication {
    Task ZoomInAsync(uint chartId);
    Task ZoomOutAsync(uint chartId);
    Task ScrollLeftAsync(uint chartId);
    Task ScrollRightAsync(uint chartId);
    IDictionary<DateTime, double> VisibleChartPoints(uint chartId);
}