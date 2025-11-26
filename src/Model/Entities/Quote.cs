using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Quote : IGuid, INotifyPropertyChanged {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime Date {
        get;
        set { field = value;  OnPropertyChanged(nameof(Date)); }
    }

    public string SecurityGuid { get; set; }

    [ForeignKey("SecurityGuid")]
    public Security Security {
        get;
        set { field = value; OnPropertyChanged(nameof(Security)); }
    }

    public double PriceInEuro {
        get;
        set { field = value; OnPropertyChanged(nameof(PriceInEuro)); }
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}