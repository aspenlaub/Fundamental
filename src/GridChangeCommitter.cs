using System.Windows.Controls;

namespace Aspenlaub.Net.GitHub.CSharp.Fundamental;

// ReSharper disable once UnusedMember.Global
public class GridChangeCommitter {
    private bool IsCommittingGridRow { get; set; }

    public GridChangeCommitter(DataGrid grid) {
        grid.CellEditEnding += CellEditEnding;
    }

    public void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
        if (IsCommittingGridRow) { return; }
        if (e.EditAction != DataGridEditAction.Commit) { return; }

        IsCommittingGridRow = true;
        ((DataGrid)sender).CommitEdit(DataGridEditingUnit.Row, true);
        IsCommittingGridRow = false;
    }
}