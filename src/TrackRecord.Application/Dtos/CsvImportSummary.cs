namespace TrackRecord.Application.Dtos;

public record CsvImportSummary(int Imported, int Skipped, IReadOnlyList<string> Errors);
