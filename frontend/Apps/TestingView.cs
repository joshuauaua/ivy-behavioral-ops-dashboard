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
            | new Card(Layout.Vertical().Gap(2) | Text.H3(events.Value.Length.ToString("N0")) | Text.P("Total Events").Muted().Small()))
        | (Layout.Horizontal().Gap(4)
            | new Button("Explore Users").WithSheet(() => usersTable, title: "Users Explorer", width: Size.Fraction(0.7f))
            | new Button("Explore Events").WithSheet(() => eventsTable, title: "Events Explorer", width: Size.Fraction(0.7f)));
  }
}
