namespace Frontend.Apps;

public class ExportDialog
{
  public record State(
      IState<bool> Show,
      IState<string> FileType,
      IState<bool> UserId,
      IState<bool> Email,
      IState<bool> SignupDate,
      IState<bool> LastSeen,
      IState<bool> Country,
      IState<string> TargetName
  );

  public static object Build(State state, IClientProvider client)
  {
    return (Layout.Vertical().Gap(6).Padding(4).Width(Size.Units(80)).Align(Align.Center)
            | (Layout.Vertical().Gap(2).Align(Align.Left)
                | Text.H3($"Export {state.TargetName.Value}")
                | Text.P("Select columns and file type.").Muted())
            | (Layout.Vertical().Gap(4).Align(Align.Center)
                | Text.P("Columns").Bold()
                | (Layout.Grid().Columns(2).Gap(6)
                    | (Layout.Horizontal().Gap(2).Align(Align.Left) | new BoolInput(state.UserId) | Text.P("user_id"))
                    | (Layout.Horizontal().Gap(2).Align(Align.Left) | new BoolInput(state.Email) | Text.P("email"))
                    | (Layout.Horizontal().Gap(2).Align(Align.Left) | new BoolInput(state.SignupDate) | Text.P("signup_date"))
                    | (Layout.Horizontal().Gap(2).Align(Align.Left) | new BoolInput(state.LastSeen) | Text.P("last_seen"))
                    | (Layout.Horizontal().Gap(2).Align(Align.Left) | new BoolInput(state.Country) | Text.P("country"))))
            | (Layout.Vertical().Gap(4).Align(Align.Center)
                | Text.P("File Type").Bold()
                | (Layout.Horizontal().Gap(2).Align(Align.Center)
                    | new Button("CSV").Variant(state.FileType.Value == "CSV" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                        .HandleClick(_ => state.FileType.Set("CSV"))
                    | new Button("JSON").Variant(state.FileType.Value == "JSON" ? ButtonVariant.Primary : ButtonVariant.Ghost)
                        .HandleClick(_ => state.FileType.Set("JSON"))))
            | new Button("Download").Variant(ButtonVariant.Primary).Icon(Icons.Download).Width(Size.Full())
                .HandleClick(_ =>
                {
                  client.Toast($"Downloading {state.FileType.Value} export for {state.TargetName.Value}...");
                  state.Show.Set(false);
                }))
        .ToDialog(state.Show);
  }
}
