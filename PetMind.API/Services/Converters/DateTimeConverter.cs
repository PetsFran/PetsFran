using System;
using System.Globalization;

public static class DateTimeConverter
{
    private static readonly TimeZoneInfo _brasiliaTimeZone = 
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    
    public static DateTime ConverterParaDateTime(string dataString)
    {
        dataString = dataString?.Trim() ?? "";
        DateTime localTime;
        
        // Tenta formato brasileiro primeiro: "dd/MM/yyyy HH:mm:ss"
        if (DateTime.TryParseExact(dataString, "dd/MM/yyyy HH:mm:ss", 
                CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out DateTime result))
        {
            localTime = result;
        }
        // Tenta formato brasileiro sem segundos: "dd/MM/yyyy HH:mm"
        else if (DateTime.TryParseExact(dataString, "dd/MM/yyyy HH:mm", 
                     CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out result))
        {
            localTime = result;
        }
        // Tenta só data: "dd/MM/yyyy"
        else if (DateTime.TryParseExact(dataString, "dd/MM/yyyy", 
                     CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out result))
        {
            localTime = result.AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute);
        }
        // Tenta parse normal (ISO)
        else if (DateTime.TryParse(dataString, CultureInfo.InvariantCulture, 
                     DateTimeStyles.None, out result))
        {
            localTime = result;
        }
        else
        {
            throw new ArgumentException(
                $"Data inválida: '{dataString}'. " +
                $"Use formato: 'dd/MM/yyyy HH:mm:ss' (ex: 25/12/2024 14:30:00)");
        }
        
        // Converte horário local (Brasília) para UTC
        // Assumindo que a data recebida está no fuso do Brasil
        DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, _brasiliaTimeZone);
        
        return utcTime;
    }
}