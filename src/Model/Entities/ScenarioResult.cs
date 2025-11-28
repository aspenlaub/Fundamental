using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class ScenarioResult(IList<Holding> scenarioStartHoldings, IList<Holding> scenarioEndHoldings) {
    public IList<Holding> ScenarioStartHoldings => scenarioStartHoldings;
    public IList<Holding> ScenarioEndHoldings => scenarioEndHoldings;

    public double SumStart() {
        return scenarioStartHoldings.Sum(h => h.QuoteValueInEuro);
    }

    public double SumEnd() {
        return scenarioEndHoldings.Sum(h => h.QuoteValueInEuro);
    }

    public double AverageYearlyChangeFactor() {
        double sumStart = SumStart();
        double sumEnd = SumEnd();
        if (sumStart == 0) {
            return double.NaN;
        }
        if (sumEnd == 0) {
            return 0;
        }

        int scenarioLengthInYears = scenarioEndHoldings.Max(h => h.Date).Year - scenarioStartHoldings.Min(h => h.Date).Year;
        double overallChangeFactor = sumEnd / sumStart;
        double yearlyChangeFactor = Math.Pow(overallChangeFactor, 1.0 / scenarioLengthInYears);
        return yearlyChangeFactor;
    }
}