namespace Frontend.Apps;

public class BuilderView : ViewBase
{
  readonly IState<Cohort[]> _cohorts;
  readonly Action<string> _navigateTo;

  public BuilderView(IState<Cohort[]> cohorts, Action<string> navigateTo)
  {
    _cohorts = cohorts;
    _navigateTo = navigateTo;
  }

  record BlockDef(string Id, string Label, string Description, Icons Icon, string Category);

  static readonly BlockDef[] AvailableBlocks =
  {
        new("user_login",      "User Login",      "Triggered when a user logs in.", Icons.LogIn, "EVENT"),
        new("purchase",        "Purchase",        "Occurs when a user completes a transaction.", Icons.ShoppingCart, "EVENT"),
        new("page_view",       "Page View",       "A user visits a specific page.", Icons.Eye, "EVENT"),
        new("user_signup",     "User Signup",     "A new user creates an account.", Icons.UserPlus, "EVENT"),
        new("region_asia",     "Region: Asia",    "Filter users located in Asia.", Icons.MapPin, "FILTER"),
        new("region_eu",       "Region: EU",      "Filter users located in Europe.", Icons.MapPin, "FILTER"),
        new("region_usa",      "Region: USA",     "Filter users located in the US.", Icons.MapPin, "FILTER"),
        new("churn_risk",      "Churn Risk",      "Users identified as likely to churn.", Icons.CircleAlert, "FILTER"),
        new("high_value",      "High Value",      "Users with high lifetime value.", Icons.CircleDollarSign, "FILTER"),
    };

  public override object? Build()
  {
    var canvasBlocks = UseState(Array.Empty<string>());
    var cohortName = UseState("New Cohort");
    var isEditingName = UseState(false);
    var client = UseService<IClientProvider>();

    // Export Dialog State
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
    var operators = UseState(Array.Empty<string>());
    var categoryFilter = UseState("All");

    // Realtime count (number of blocks added as a proxy)
    int count = canvasBlocks.Value.Length * 1247; // simulated count

    var titleContent = isEditingName.Value
        ? Layout.Horizontal().Gap(2).Align(Align.Left)
            | new TextInput(cohortName, placeholder: "Cohort name...")
            | new Button().Icon(Icons.Check).Variant(ButtonVariant.Ghost).HandleClick(_ => isEditingName.Set(false))
        : Layout.Horizontal().Gap(2).Align(Align.Left)
            | Text.H3(cohortName.Value)
            | new Button().Icon(Icons.Pencil).Variant(ButtonVariant.Ghost).HandleClick(_ => isEditingName.Set(true));

    var headerCard = new Card(
        Layout.Vertical().Gap(2).Align(Align.Left).Width(Size.Full())
            | (Layout.Vertical().Gap(1).Align(Align.Left)
                | titleContent
                | Text.P("Define your target audience by combining filters.").Muted())
            | (Layout.Horizontal().Gap(4).Align(Align.Center).Width(Size.Full())
                | (Layout.Vertical().Gap(2)
                    | (Layout.Horizontal().Gap(2).Width(Size.Full())
                        | new Button("Save Cohort").Variant(ButtonVariant.Primary).Icon(Icons.Save).Width(Size.Units(60))
                            .HandleClick(_ =>
                            {
                              var newCohort = new Cohort(
                                  cohortName.Value,
                                  1,
                                  count,
                                  DateTime.Now.ToString("yyyy-MM-dd"),
                                  "New",
                                  canvasBlocks.Value,
                                  operators.Value
                              );
                              _cohorts.Set([.. _cohorts.Value, newCohort]);
                              client.Toast("Cohort saved!");
                              _navigateTo("library");
                            })
                        | new Button("Export").Variant(ButtonVariant.Outline).Icon(Icons.Download).Width(Size.Units(60))
                            .HandleClick(_ =>
                            {
                              exportState.TargetName.Set(cohortName.Value);
                              exportState.Show.Set(true);
                            }))
                    | (Layout.Horizontal().Gap(2).Width(Size.Full())
                        | new Button("Schedule").Variant(ButtonVariant.Outline).Icon(Icons.Clock).Width(Size.Units(60))
                            .HandleClick(_ => client.Toast("Schedule dialog coming soon"))
                        | new Button("Clear").Variant(ButtonVariant.Destructive).Icon(Icons.Trash2).Width(Size.Units(60))
                            .HandleClick(_ => canvasBlocks.Set(Array.Empty<string>()))))
                | new Card(
                    Layout.Vertical().Gap(1).Align(Align.Center)
                        | Text.H2(count > 0 ? count.ToString("N0") : "—")
                        | Text.P("Realtime Count").Muted().Small()
                  ))
    );


    // --- Sidebar block palette ---
    var sidebarContent = Layout.Vertical().Gap(2)
        | AvailableBlocks
            .Where(b => (categoryFilter.Value == "All" || b.Category == categoryFilter.Value) && !canvasBlocks.Value.Contains(b.Id))
            .Select(b =>
                (object)new Card(
                    Layout.Horizontal().Gap(2).Align(Align.Center).Padding(1, 4)
                        | (Layout.Vertical().Gap(1)
                            | Text.P(b.Label).Small()
                            | Text.P(b.Category).Small().Muted().Color(Colors.Purple))
                        | new Spacer()
                        | new Button("+")
                            .Variant(ButtonVariant.Ghost)
                            .HandleClick(_ =>
                            {
                              if (canvasBlocks.Value.Length >= 1)
                              {
                                operators.Set([.. operators.Value, "AND"]);
                              }
                              canvasBlocks.Set([.. canvasBlocks.Value, b.Id]);
                            })
                ).WithTooltip(b.Description));

    // --- Canvas card ---
    IEnumerable<object> canvasItems = canvasBlocks.Value.Select((blockId, idx) =>
    {
      var def = AvailableBlocks.FirstOrDefault(b => b.Id == blockId)
                            ?? new BlockDef(blockId, blockId, "", Icons.Box, "EVENT");
      var capturedIdx = idx;

      var blockCard = (object)(Layout.Horizontal().Width(Size.Units(100)).Align(Align.Center)
        | new Card(
              Layout.Horizontal().Gap(2).Align(Align.Center).Padding(2)
                  | (Layout.Vertical().Gap(0)
                      | Text.P(def.Label).Bold().Small()
                      | Text.P(def.Category).Small().Muted().Color(Colors.Purple))
                  | new Spacer()
                  | new Button().Icon(Icons.X).Variant(ButtonVariant.Ghost)
                      .HandleClick(_ =>
                      {
                        if (capturedIdx > 0 && operators.Value.Length > capturedIdx - 1)
                        {
                          operators.Set(operators.Value.Where((_, i) => i != capturedIdx - 1).ToArray());
                        }
                        else if (operators.Value.Length > 0)
                        {
                          operators.Set(operators.Value.Skip(1).ToArray());
                        }
                        canvasBlocks.Set(canvasBlocks.Value.Where((_, i) => i != capturedIdx).ToArray());
                      })));

      if (idx > 0 && idx - 1 < operators.Value.Length)
      {
        var opIdx = idx - 1;
        var op = operators.Value[opIdx];
        var opSelector = (object)(Layout.Horizontal().Gap(2).Align(Align.Center)
            | new Button("AND")
                .Variant(op == "AND" ? ButtonVariant.Primary : ButtonVariant.Outline)
                .HandleClick(_ =>
                {
                  var newOps = operators.Value.ToArray();
                  newOps[opIdx] = "AND";
                  operators.Set(newOps);
                })
            | new Button("OR")
                .Variant(op == "OR" ? ButtonVariant.Primary : ButtonVariant.Outline)
                .HandleClick(_ =>
                {
                  var newOps = operators.Value.ToArray();
                  newOps[opIdx] = "OR";
                  operators.Set(newOps);
                }));

        return new object[] { opSelector, blockCard };
      }

      return [blockCard];
    }).SelectMany(x => (object[])x);

    object canvasInner;
    if (canvasBlocks.Value.Length == 0)
    {
      canvasInner = Layout.Center()
          | (Layout.Vertical().Gap(6).Align(Align.Center)
              | new Icon(Icons.LayoutTemplate)
              | Text.H3("No blocks added yet")
              | Text.P("Click '+' on any block in the sidebar to add it here.").Muted());
    }
    else
    {
      canvasInner = Layout.Vertical().Gap(4).Align(Align.Center).Width(Size.Half())
          | canvasItems;
    }

    var canvasCard = new Card(
        Layout.Center().Height(Size.Units(126))
            | canvasInner
    );

    // --- Export Dialog ---
    var exportDialog = ExportDialog.Build(exportState, client);

    // --- Cohort Insights ---
    object? cohortInsights = null;
    if (canvasBlocks.Value.Length > 0)
    {
      var demoData = new[]
      {
            new { Region = "Asia",  Value = 15 },
            new { Region = "EU",    Value = 25 },
            new { Region = "Other", Value = 10 },
            new { Region = "USA",   Value = 50 },
        };

      var eventsData = new[]
      {
            new { Event = "Purchase",    Count = 180 },
            new { Event = "Sign Up",     Count = 230 },
            new { Event = "Page View",   Count = 1580 },
            new { Event = "Add to Cart", Count = 480 },
        };

      cohortInsights = Layout.Vertical().Gap(4)
              | (Layout.Vertical().Gap(1)
                  | Text.H3("Cohort Insights")
                  | Text.P("An overview of the users in your defined cohort.").Muted())
              | (Layout.Grid().Columns(2).Gap(4)
                  | new Card(
                      Layout.Vertical().Gap(2)
                          | Text.P("Demographic Breakdown").Bold()
                          | demoData.ToPieChart(e => e.Region, e => (double)e.Sum(f => f.Value)))
                  | new Card(
                      Layout.Vertical().Gap(2)
                          | Text.P("Top Events").Bold()
                          | eventsData.ToBarChart()
                              .Dimension("Event", e => e.Event)
                              .Measure("Count", e => (double)e.Sum(f => f.Count))));
    }

    // --- Main content: header + canvas + insights ---
    var mainContent = Layout.Vertical().Gap(2).Width(Size.Full())
        | headerCard
        | canvasCard
        | cohortInsights;

    var layout = new SidebarLayout(
        mainContent: mainContent,
        sidebarContent: sidebarContent,
        sidebarHeader: Layout.Vertical().Gap(2)
            | Text.Lead("Block")
            | new SelectInput<string>(categoryFilter, new[] { "All", "EVENT", "FILTER" }.ToOptions())
    );

    return (Layout.Vertical()
        | layout
        | exportDialog);
  }
}
