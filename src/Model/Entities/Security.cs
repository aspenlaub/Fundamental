using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Security : IGuid, INotifyPropertyChanged, ISecurity {
    [Key]
    public string Guid { get; set; }

    private string _PrivateSecurityId;
    [MaxLength(12)]
    public string SecurityId { get => _PrivateSecurityId; set { _PrivateSecurityId = value; OnPropertyChanged(nameof(SecurityId)); } }

    private string _PrivateSecurityName;
    [MaxLength(64)]
    public string SecurityName { get => _PrivateSecurityName; set { _PrivateSecurityName = value; OnPropertyChanged(nameof(SecurityName)); } }

    private double _PrivateQuotedPer;
    public double QuotedPer { get => _PrivateQuotedPer; set { _PrivateQuotedPer = value; OnPropertyChanged(nameof(QuotedPer)); } }

    [InverseProperty("Security")]
    public ICollection<Holding> Holdings { get; set; }

    [InverseProperty("Security")]
    public ICollection<Quote> Quotes { get; set; }

    [InverseProperty("Security")]
    public ICollection<Transaction> Transactions { get; set; }

    public Security() {
        Guid = System.Guid.NewGuid().ToString();
    }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}