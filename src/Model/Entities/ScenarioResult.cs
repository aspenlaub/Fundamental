using System.Collections.Generic;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental.Model.Entities;

public class ScenarioResult(IList<Holding> scenarioStartHoldings, IList<Holding> scenarioEndHoldings) {
    public IList<Holding> ScenarioStartHoldings => scenarioStartHoldings;
    public IList<Holding> ScenarioEndHoldings => scenarioEndHoldings;
}
