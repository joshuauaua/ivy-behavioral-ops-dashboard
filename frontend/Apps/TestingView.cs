namespace Frontend.Apps;

using System.Net.Http;
using System.Text.Json;
using System.Linq;

public class TestingView : ViewBase
{
  public override object? Build()
  {
    var users = UseState(new DataTableUser[0]);
    var events = UseState(new DataTableEvent[0]);
    var googleActions = UseState(new GoogleTestAction[0]);
    var isLoading = UseState(true);
    var errorMsg = UseState((string?)null);

    UseEffect(async () =>
    {
      try
      {
        isLoading.Set(true);
        using var http = new HttpClient();

        var usersJson = await http.GetStringAsync("http://localhost:5152/api/analytics/users");
        var usersData = JsonSerializer.Deserialize<DataTableUser[]>(usersJson);
        if (usersData != null) users.Set(usersData);

        var eventsJson = await http.GetStringAsync("http://localhost:5152/api/analytics/events");
        var eventsData = JsonSerializer.Deserialize<DataTableEvent[]>(eventsJson);
        if (eventsData != null) events.Set(eventsData);

        var googleJson = await http.GetStringAsync("http://localhost:5152/api/analytics/google-sheets");
        var googleData = JsonSerializer.Deserialize<GoogleTestAction[]>(googleJson);
        if (googleData != null) googleActions.Set(googleData);

        errorMsg.Set((string?)null);
      }
      catch (Exception ex)
      {
        errorMsg.Set(ex.Message);
      }
      finally
      {
        isLoading.Set(false);
      }
    }, []);

    var usersTable = users.Value.AsQueryable().ToDataTable(u => u.id)
        .Header(u => u.externalId, "User ID")
        .Header(u => u.email, "Email")
        .Header(u => u.region, "Region")
        .Header(u => u.country, "Country")
        .Header(u => u.signupDate, "Signed Up")
        .Header(u => u.highValue, "High Value")
        .Header(u => u.churnRisk, "Churn Risk")
        .Header(u => u.onboardingFunnelBlock, "Funnel Progress")
        .Config(config =>
        {
          config.ShowSearch = true;
          config.AllowFiltering = true;
          config.BatchSize = 20;
        })
        .Height(Size.Units(120));

    var eventsTable = events.Value.AsQueryable().ToDataTable(e => e.id)
        .Header(e => e.userEmail, "User")
        .Header(e => e.type, "Event Type")
        .Header(e => e.timestamp, "Timestamp")
        .Header(e => e.metadata, "Metadata")
        .Config(config =>
        {
          config.ShowSearch = true;
          config.AllowFiltering = true;
          config.BatchSize = 20;
        })
        .Height(Size.Units(120));

    var googleTable = googleActions.Value.AsQueryable().ToDataTable(g => g.id)
        .Header(g => g.id, "Google Test Action ID")
        .Header(g => g.action, "Google Test Action")
        .Header(g => g.dateTime, "Google Test DateTime")
        .Config(config =>
        {
          config.ShowSearch = true;
          config.AllowFiltering = true;
          config.BatchSize = 20;
        })
        .Height(Size.Units(120));

    if (isLoading.Value)
      return Layout.Center() | Text.P("Loading system data...").Muted();

    if (errorMsg.Value != null)
      return Layout.Center() | (Layout.Vertical().Gap(2) | Text.P("Connection Error").Bold() | Text.P(errorMsg.Value).Small().Muted());

    return Layout.Vertical().Gap(6).Padding(6)
        | (Layout.Vertical().Gap(1)
            | Text.H2("System Data Explorer")
            | Text.P("Browse raw data via side panels.").Muted())
        | (Layout.Horizontal().Gap(4).Align(Align.Center)
            | new Card(Layout.Vertical().Gap(2) | Text.H3(users.Value.Length.ToString("N0")) | Text.P("Total Users").Muted().Small())
            | new Card(Layout.Vertical().Gap(2) | Text.H3(events.Value.Length.ToString("N0")) | Text.P("Total Events").Muted().Small())
            | new Card(Layout.Vertical().Gap(2) | Text.H3(googleActions.Value.Length.ToString("N0")) | Text.P("Google Actions").Muted().Small()))
        | (Layout.Horizontal().Gap(4)
            | new Button("Explore Users").WithSheet(() => usersTable, title: "Users Explorer", width: Size.Fraction(0.7f))
            | new Button("Explore Events").WithSheet(() => eventsTable, title: "Events Explorer", width: Size.Fraction(0.7f))
            | new Button("Explore Google Actions").WithSheet(() => googleTable, title: "Google Test Actions", width: Size.Fraction(0.7f)))
        | (Layout.Vertical().Gap(4)
            | (Layout.Vertical().Gap(1)
                | Text.H3("External Integrations")
                | Text.P("Diagnostic tools for third-party APIs.").Muted().Small())
            | (Layout.Horizontal().Gap(4)
                | new Card(Layout.Vertical().Gap(4)
                    | Text.P("Heyreach Status").Bold()
                    | new Button("Check Connectivity").OnClick(async () =>
                    {
                      using var http = new HttpClient();
                      try
                      {
                        var resp = await http.GetAsync("http://localhost:5152/api/heyreach/status");
                        var content = await resp.Content.ReadAsStringAsync();
                        Alert.Show("Heyreach Status", content);
                      }
                      catch (Exception ex)
                      {
                        Alert.Show("Connection Error", ex.Message);
                      }
                    }))
                | new Card(Layout.Vertical().Gap(4).Width(Size.Half())
                    | Text.P("Fetch Sample Campaign").Bold()
                    | (Layout.Horizontal().Gap(2)
                        | new InputText("Campaign ID", "274079").Id("campaign_id_input")
                        | new Button("Fetch").OnClick(async () =>
                        {
                          var input = View.GetElementById<InputText>("campaign_id_input");
                          var id = input?.Value ?? "274079";
                          using var http = new HttpClient();
                          try
                          {
                            var resp = await http.GetAsync($"http://localhost:5152/api/heyreach/campaign/{id}");
                            var content = await resp.Content.ReadAsStringAsync();
                            Alert.Show("Campaign Details", content);
                          }
                          catch (Exception ex)
                          {
                            Alert.Show("Fetch Error", ex.Message);
                          }
                        }))))
        );
  }
}
