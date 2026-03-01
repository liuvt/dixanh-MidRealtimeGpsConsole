namespace MidRealtimeGpsConsole.Models;

public sealed class GoogleSheetsOptions
{
    public string SpreadsheetId { get; set; } = "";
    public string SheetName { get; set; } = "KET_QUA";
    public string ServiceAccountJsonPath { get; set; } = "";
    public string ClearRange { get; set; } = "A2:Z"; // vùng data
}
