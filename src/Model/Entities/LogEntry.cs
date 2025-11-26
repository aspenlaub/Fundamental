using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class LogEntry : IGuid, INotifyPropertyChanged {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime LogTime {
        get;
        set { field = value; OnPropertyChanged(nameof(LogTime)); }
    }

    public string LogType {
        get;
        set { field = value; OnPropertyChanged(nameof(LogType)); }
    }

    public string LogMessage {
        get;
        set { field = value; OnPropertyChanged(nameof(LogMessage)); }
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}