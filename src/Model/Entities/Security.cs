using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMember.Global
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class Security : IGuid, INotifyPropertyChanged, ISecurity {
    [Key]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    [MaxLength(12)]
    public string SecurityId {
        get;
        set { field = value; OnPropertyChanged(nameof(SecurityId)); }
    }

    [MaxLength(64)]
    public string SecurityName {
        get;
        set { field = value; OnPropertyChanged(nameof(SecurityName)); }
    }

    public double QuotedPer {
        get;
        set { field = value; OnPropertyChanged(nameof(QuotedPer)); }
    }

    [InverseProperty("Security")]
    public ICollection<Holding> Holdings { get; set; }

    [InverseProperty("Security")]
    public ICollection<Quote> Quotes { get; set; }

    [InverseProperty("Security")]
    public ICollection<Transaction> Transactions { get; set; }

    protected void OnPropertyChanged(string propertyName) {
        // ReSharper disable once UseNullPropagation
        if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}