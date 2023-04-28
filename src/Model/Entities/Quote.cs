using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Quote : IGuid, INotifyPropertyChanged {
    [Key]
    public string Guid { get; set; }

    private DateTime _PrivateDate;
    public DateTime Date {  get => _PrivateDate; set { _PrivateDate = value;  OnPropertyChanged(nameof(Date)); } }

    public string SecurityGuid { get; set; }
    private Security _PrivateSecurity;
    [ForeignKey("SecurityGuid")]
    public Security Security { get => _PrivateSecurity; set { _PrivateSecurity = value; OnPropertyChanged(nameof(Security)); } }

    private double _PrivatePriceInEuro;
    public double PriceInEuro { get => _PrivatePriceInEuro; set { _PrivatePriceInEuro = value; OnPropertyChanged(nameof(PriceInEuro)); } }

    public Quote() {
        Guid = System.Guid.NewGuid().ToString();
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}