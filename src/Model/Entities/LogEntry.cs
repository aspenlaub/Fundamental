using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class LogEntry : IGuid, INotifyPropertyChanged {
    [Key]
    public string Guid { get; set; }

    private DateTime _PrivateLogTime;
    public DateTime LogTime { get => _PrivateLogTime; set { _PrivateLogTime = value; OnPropertyChanged(nameof(LogTime)); } }

    private string _PrivateLogType;
    public string LogType { get => _PrivateLogType; set { _PrivateLogType = value; OnPropertyChanged(nameof(LogType)); } }

    private string _PrivateLogMessage;
    public string LogMessage { get => _PrivateLogMessage; set { _PrivateLogMessage = value; OnPropertyChanged(nameof(LogMessage)); } }

    public LogEntry() {
        Guid = System.Guid.NewGuid().ToString();
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}