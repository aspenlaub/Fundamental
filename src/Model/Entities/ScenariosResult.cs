using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class ScenariosResult {
    private readonly List<double> _YearlyChangeFactors = [];
    public double MinimumAverageYearlyChangeFactor { get; private set; }
    public double MedianAverageYearlyChangeFactor { get; private set; }
    public double MaximumAverageYearlyChangeFactor { get; private set; }
    public bool ResultsChangedAfterLatestAddition { get; private set; }

    private const double _epsilon = 1E-8;

    public void Add(double yearlyChangeFactor) {
        _YearlyChangeFactors.Add(yearlyChangeFactor);

        _YearlyChangeFactors.Sort();
        ResultsChangedAfterLatestAddition = false;
        double newMinimumAverageYearlyChangeFactor = _YearlyChangeFactors.Min();
        ResultsChangedAfterLatestAddition =
            ResultsChangedAfterLatestAddition
            || Math.Abs(MinimumAverageYearlyChangeFactor - newMinimumAverageYearlyChangeFactor) > _epsilon;
        MinimumAverageYearlyChangeFactor = newMinimumAverageYearlyChangeFactor;
        double newMedianAverageYearlyChangeFactor = _YearlyChangeFactors[_YearlyChangeFactors.Count / 2];
        ResultsChangedAfterLatestAddition =
            ResultsChangedAfterLatestAddition
            || Math.Abs(MedianAverageYearlyChangeFactor- newMedianAverageYearlyChangeFactor) > _epsilon;
        MedianAverageYearlyChangeFactor = newMedianAverageYearlyChangeFactor;
        double newMaximumAverageYearlyChangeFactor = _YearlyChangeFactors.Max();
        ResultsChangedAfterLatestAddition =
            ResultsChangedAfterLatestAddition
            || Math.Abs(MaximumAverageYearlyChangeFactor - newMaximumAverageYearlyChangeFactor) > _epsilon;
        MaximumAverageYearlyChangeFactor = newMaximumAverageYearlyChangeFactor;
    }

    public int NumberOfDistinctChangeFactors() {
        return _YearlyChangeFactors.Distinct().Count();
    }
}
