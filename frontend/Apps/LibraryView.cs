namespace Frontend.Apps;

public class LibraryView : ViewBase
{
  readonly IState<Cohort[]> _cohorts;
  readonly Action<string> _navigateTo;

  public LibraryView(IState<Cohort[]> cohorts, Action<string> navigateTo)
  {
    _cohorts = cohorts;
    _navigateTo = navigateTo;
  }

  public override object? Build()
  {
    var search = UseState("");
    var activeFilter = UseState("All");
    var client = UseService<IClientProvider>();
    var exportState = new ExportDialog.State(
        UseState(false),
        UseState("CSV"),
        UseState(true),
        UseState(true),
        UseState(true),
        UseState(false),
        UseState(false),
        UseState("")
    );

    string[] filters = { "All", "New", "High-Value", "Archived", "Marketing" };

    // Filter cohorts
    var filtered = _cohorts.Value
        .Where(c => (activeFilter.Value == "All" || c.Tag == activeFilter.Value)
                 && (search.Value == "" || c.Name.Contains(search.Value, StringComparison.OrdinalIgnoreCase)))
        .ToArray();

    // --- Top bar ---
    var topBar = Layout.Horizontal().Gap(4).Align(Align.Center).Width(Size.Full())
        | (Layout.Vertical().Gap(1)
            | Text.H2("Cohort Library")
            | Text.P("Browse, search, and manage your saved cohorts.").Muted())
        | new Spacer()
        | new Button("+ New Cohort")
            .Variant(ButtonVariant.Primary)
            .HandleClick(_ => _navigateTo("builder"));

    // --- Search + filter row ---
    var searchAndFilter = Layout.Horizontal().Gap(4).Align(Align.Center).Width(Size.Full())
        | new TextInput(search, placeholder: "Search cohorts...", variant: TextInputs.Search)
        | new Spacer()
        | (Layout.Horizontal().Gap(2)
            | filters.Select(f =>
                (object)new Button(f)
                    .Variant(activeFilter.Value == f ? ButtonVariant.Secondary : ButtonVariant.Ghost)
                    .HandleClick(_ => activeFilter.Value = f)));

    // --- Cohort grid ---
    object grid;
    if (filtered.Length == 0)
    {
      grid = Layout.Center()
          | (Layout.Vertical().Gap(3).Align(Align.Center)
              | new Icon(Icons.FolderOpen)
              | Text.H4("No cohorts found")
              | Text.P("Try adjusting your search or filters.").Muted());
    }
    else
    {
      grid = Layout.Grid().Columns(2).Gap(4)
          | filtered.Select(c =>
              (object)new Card(
                  Layout.Vertical().Gap(3)
                      | (Layout.Horizontal().Gap(3).Align(Align.Center)
                          | (Layout.Vertical().Gap(1)
                              | (Layout.Horizontal().Gap(2).Align(Align.Center)
                                  | Text.P(c.Name)
                                  | Text.P($"v{c.Version}").Muted().Small())
                              | new Badge(c.Tag))
                          | new Spacer()
                          | (Layout.Vertical().Gap(1).Align(Align.Right)
                              | Text.P($"{c.Members:N0} members").Color(Colors.Blue)
                              | Text.P($"Refreshed: {c.RefreshedDate}").Muted().Small()))
                      | (Layout.Horizontal().Gap(2)
                          | new Button("Open").Variant(ButtonVariant.Outline)
                              .HandleClick(_ => _navigateTo("builder"))
                          | new Button("Export").Variant(ButtonVariant.Outline)
                              .HandleClick(_ =>
                              {
                                exportState.TargetName.Set(c.Name);
                                exportState.Show.Set(true);
                              }))
              ));
    }

    return Layout.Vertical().Gap(6).Padding(6)
        | topBar
        | searchAndFilter
        | grid
        | ExportDialog.Build(exportState, client);
  }
}
