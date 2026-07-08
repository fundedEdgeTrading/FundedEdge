namespace TrackRecord.Infrastructure.Integrations.Tradovate;

public class TradovateApiException(string message, Exception? inner = null) : Exception(message, inner);
